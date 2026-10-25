using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.Invoices.Queries.GetOutstandingDebts;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Appointments;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Doctors;
using DentalClinic.Domain.Invoices;
using DentalClinic.Domain.Invoices.Enums;
using DentalClinic.Domain.Patients;
using DentalClinic.Domain.Services;
using MediatR;

using Microsoft.EntityFrameworkCore;

using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Invoices.Queries.GetOutstandingDebts;

[Collection(WebAppFactoryCollection.CollectionName)]
public class GetOutstandingDebtsQueryHandlerTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    [Fact]
    public async Task Handle_ShouldGroupPerPatient_ExcludePaidAndDrafts_AndOrderByAmount()
    {
        // Arrange
        // ليست هناك إعادة ضبط بين الاختبارات في هذه المجموعة، لذا ننظّف
        // بيانات الفاتورة أولاً حتى لا تؤثر فواتير اختبارات أخرى على الاستعلام
        // الذي لا يرشّح حسب مريض معيّن.
        await ClearInvoiceDataAsync();
        var (inDebtPatient, fullyPaidPatient) = await SeedPairAsync();

        // 250 total left: 200 unpaid + 50 left of a 150 partially-paid invoice.
        var unpaid = Invoice.Create(inDebtPatient.Id, 200).Value;
        var partial = Invoice.Create(inDebtPatient.Id, 150).Value;
        partial.RegisterPayment(100);

        // 80 outstanding — a second debtor so the ordering can be asserted.
        var smallerDebt = Invoice.Create(fullyPaidPatient.Id, 80).Value;

        // Fully paid invoice is not debt.
        var paid = Invoice.Create(fullyPaidPatient.Id, 300).Value;
        paid.RegisterPayment(300);

        // Drafts are estimates, not debt.
        var draft = Invoice.Create(fullyPaidPatient.Id, 175, null, InvoiceLifecycleStatus.Draft);
        Assert.True(draft.IsSuccess);

        await _context.Invoices.AddRangeAsync(unpaid, partial, smallerDebt, paid, draft.Value);
        await _context.SaveChangesAsync(default);

        var query = new GetOutstandingDebtsQuery(Scope: DoctorAccessScope.None, PageNumber: 1, PageSize: 20);

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        var debts = result.Value.Items!.ToList();
        Assert.Equal(2, debts.Count);

        // Bigger balances first; ties fall back to the oldest invoice.
        Assert.Equal(inDebtPatient.Id, debts[0].PatientId);
        Assert.Equal(250, debts[0].TotalOutstanding);
        Assert.Equal(2, debts[0].OpenInvoiceCount);
        Assert.Equal("John Doe", debts[0].PatientName);
        Assert.NotNull(debts[0].OldestOpenInvoiceDateUtc);

        Assert.Equal(fullyPaidPatient.Id, debts[1].PatientId);
        Assert.Equal(80, debts[1].TotalOutstanding);
        Assert.Equal(1, debts[1].OpenInvoiceCount);
    }

    [Fact]
    public async Task Handle_WithMinAmount_ShouldFilterSingleInvoices()
    {
        // Arrange: only the 200 invoice survives the min-amount threshold — the
        // 100 invoice cannot qualify on its own despite the same patient.
        await ClearInvoiceDataAsync();
        var (highDebtPatient, lowDebtPatient) = await SeedPairAsync();

        var big = Invoice.Create(highDebtPatient.Id, 200).Value;
        var small = Invoice.Create(lowDebtPatient.Id, 100).Value;

        await _context.Invoices.AddRangeAsync(big, small);
        await _context.SaveChangesAsync(default);

        var query = new GetOutstandingDebtsQuery(Scope: DoctorAccessScope.None, MinAmount: 150, PageNumber: 1, PageSize: 20);

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        var debts = result.Value.Items!.ToList();
        Assert.Single(debts);
        Assert.Equal(highDebtPatient.Id, debts[0].PatientId);
    }

    [Fact]
    public async Task Handle_WhenEveryInvoiceIsPaid_ShouldReturnEmptyList()
    {
        // Arrange
        await ClearInvoiceDataAsync();
        var (patient, _) = await SeedPairAsync();

        var paid = Invoice.Create(patient.Id, 220).Value;
        paid.RegisterPayment(220);

        await _context.Invoices.AddAsync(paid);
        await _context.SaveChangesAsync(default);

        // The page size differs from the first test so no cached result leaks in.
        var query = new GetOutstandingDebtsQuery(Scope: DoctorAccessScope.None, PageNumber: 1, PageSize: 30);

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items!);
        Assert.Equal(0, result.Value.TotalCount);
    }

    [Fact]
    public async Task Handle_WithRestrictedDoctorScope_ShouldHideStandaloneInvoices()
    {
        // Arrange
        var (patient, doctor, service) = await SeedWithServiceAsync();

        var appointment = Appointment.Create(patient.Id, doctor.Id, service.Id, DateTime.UtcNow.AddHours(1), 30, null).Value;
        await _context.Appointments.AddAsync(appointment);
        await _context.SaveChangesAsync(default);

        var linked = Invoice.Create(patient.Id, 90, appointment.Id).Value;
        var standalone = Invoice.Create(patient.Id, 700).Value;

        await _context.Invoices.AddRangeAsync(linked, standalone);
        await _context.SaveChangesAsync(default);

        var query = new GetOutstandingDebtsQuery(
            Scope: new DoctorAccessScope(IsRestricted: true, DoctorId: doctor.Id),
            PageNumber: 1,
            PageSize: 40);

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        var debts = result.Value.Items!.ToList();
        Assert.Single(debts);
        Assert.Equal(90, debts[0].TotalOutstanding);
    }

    private async Task ClearInvoiceDataAsync()
    {
        // This collection does not reset the database between tests, so the
        // unfiltered (whole-clinic) debt queries must start from empty tables.
        await _context.Payments.ExecuteDeleteAsync();
        await _context.Invoices.ExecuteDeleteAsync();
    }

    private async Task<(Patient InDebtPatient, Patient OtherPatient)> SeedPairAsync()
    {
        // نسخة ContactInfo مستقلة لكل كيان: مشاركتها بين مريضيْن تجعل EF
        // يُدرج أعمدةَ المالك الثاني NULL (owned type لا يُشارَك بين مالكيْن من النوع نفسه).
        var first = Patient.Create("John", "Doe",
            ContactInfo.Create("+963911111111", null, true, null).Value,
            DateTime.UtcNow.AddYears(-30), Gender.Male).Value;
        var second = Patient.Create("Alan", "Smee",
            ContactInfo.Create("+963922222222", null, true, null).Value,
            DateTime.UtcNow.AddYears(-20), Gender.Male).Value;

        await _context.Patients.AddRangeAsync(first, second);
        await _context.SaveChangesAsync(default);

        return (first, second);
    }

    private async Task<(Patient Patient, Doctor Doctor, DentalService Service)> SeedWithServiceAsync()
    {
        var patient = Patient.Create("Reem", "Nasr",
            ContactInfo.Create("+963911111111", null, true, null).Value,
            DateTime.UtcNow.AddYears(-35), Gender.Female).Value;
        var doctor = Doctor.Create("Omar", "Ali", "Orthodontics",
            ContactInfo.Create("+963922222222", null, true, null).Value, Gender.Male).Value;
        var service = DentalService.Create("Teeth Cleaning", "A routine dental cleaning procedure.", 30).Value;

        await _context.Patients.AddAsync(patient);
        await _context.Doctors.AddAsync(doctor);
        await _context.DentalServices.AddAsync(service);
        await _context.SaveChangesAsync(default);

        return (patient, doctor, service);
    }
}
