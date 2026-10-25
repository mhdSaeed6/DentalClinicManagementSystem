using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.Invoices.Queries.GetInvoiceSummary;
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
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Invoices.Queries.GetInvoiceSummary;

[Collection(WebAppFactoryCollection.CollectionName)]
public class GetInvoiceSummaryQueryHandlerTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    [Fact]
    public async Task Handle_ShouldAggregateIssuedInvoicesAndExcludeDrafts()
    {
        // Arrange
        var patient = await SeedPatientAsync();

        var unpaid = Invoice.Create(patient.Id, 200).Value;
        var partiallyPaid = Invoice.Create(patient.Id, 150).Value;
        partiallyPaid.RegisterPayment(50);
        var paid = Invoice.Create(patient.Id, 100).Value;
        paid.RegisterPayment(100);

        // مسودة ليست ديناً حقيقيّاً: يجب استثناؤها من كل الإجماليات
        var draft = Invoice.Create(patient.Id, 175, null, InvoiceLifecycleStatus.Draft);
        Assert.True(draft.IsSuccess);

        await _context.Invoices.AddRangeAsync(unpaid, partiallyPaid, paid, draft.Value);
        await _context.SaveChangesAsync(default);

        var query = new GetInvoiceSummaryQuery(Scope: DoctorAccessScope.None, PatientId: patient.Id);

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        var summary = result.Value;

        // Drafts (issued and otherwise) never take part in the money totals.
        Assert.Equal(3, summary.TotalInvoiceCount);
        Assert.Equal(450, summary.TotalBilled);
        Assert.Equal(150, summary.TotalPaid);
        Assert.Equal(300, summary.TotalOutstanding);
        Assert.Equal(2, summary.OpenInvoiceCount);
        Assert.NotNull(summary.OldestUnpaidInvoiceDateUtc);
    }

    [Fact]
    public async Task Handle_ShouldSumOverTheWholeSet_NeverOverAPage()
    {
        // Arrange: more invoices than any used page size — the aggregate must
        // still cover them all (AUDIT.md D-09 / §6.8 client-side sum bug).
        var patient = await SeedPatientAsync();

        var invoices = new List<Invoice>();
        invoices.Add(Invoice.Create(patient.Id, 100).Value);
        invoices.Add(Invoice.Create(patient.Id, 100).Value);
        invoices.Add(Invoice.Create(patient.Id, 100).Value);
        invoices.Add(Invoice.Create(patient.Id, 100).Value);

        await _context.Invoices.AddRangeAsync(invoices);
        await _context.SaveChangesAsync(default);

        var query = new GetInvoiceSummaryQuery(Scope: DoctorAccessScope.None, PatientId: patient.Id);

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value.TotalInvoiceCount);
        Assert.Equal(400, result.Value.TotalBilled);
        Assert.Equal(400, result.Value.TotalOutstanding);
    }

    [Fact]
    public async Task Handle_WithRestrictedDoctorScope_ShouldOnlyCountInvoicesThroughThatDoctorsAppointments()
    {
        // Arrange
        var (patient, ownDoctor, otherDoctor, service) = await SeedEntitiesAsync();

        var ownAppointment = Appointment.Create(patient.Id, ownDoctor.Id, service.Id, DateTime.UtcNow.AddHours(1), 30, null).Value;
        var otherAppointment = Appointment.Create(patient.Id, otherDoctor.Id, service.Id, DateTime.UtcNow.AddHours(2), 30, null).Value;
        await _context.Appointments.AddRangeAsync(ownAppointment, otherAppointment);
        await _context.SaveChangesAsync(default);

        var ownInvoice = Invoice.Create(patient.Id, 120, ownAppointment.Id).Value;
        var otherInvoice = Invoice.Create(patient.Id, 340, otherAppointment.Id).Value;

        // No appointment -> never visible in a restricted scope.
        var standalone = Invoice.Create(patient.Id, 500).Value;

        await _context.Invoices.AddRangeAsync(ownInvoice, otherInvoice, standalone);
        await _context.SaveChangesAsync(default);

        var query = new GetInvoiceSummaryQuery(
            Scope: new DoctorAccessScope(IsRestricted: true, DoctorId: ownDoctor.Id),
            PatientId: patient.Id);

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalInvoiceCount);
        Assert.Equal(120, result.Value.TotalBilled);
        Assert.Equal(120, result.Value.TotalOutstanding);
    }

    [Fact]
    public async Task Handle_WhenPatientHasNoIssuedInvoices_ShouldReturnZeroes()
    {
        // Arrange
        var patient = await SeedPatientAsync();

        var query = new GetInvoiceSummaryQuery(Scope: DoctorAccessScope.None, PatientId: patient.Id);

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.TotalInvoiceCount);
        Assert.Equal(0, result.Value.TotalBilled);
        Assert.Equal(0, result.Value.TotalPaid);
        Assert.Equal(0, result.Value.TotalOutstanding);
        Assert.Equal(0, result.Value.OpenInvoiceCount);
        Assert.Null(result.Value.OldestUnpaidInvoiceDateUtc);
    }

    private async Task<Patient> SeedPatientAsync()
    {
        var contactInfo = ContactInfo.Create("+963911111111", null, true, null).Value;
        var patient = Patient.Create("John", "Doe", contactInfo, DateTime.UtcNow.AddYears(-30), Gender.Male).Value;

        await _context.Patients.AddAsync(patient);
        await _context.SaveChangesAsync(default);

        return patient;
    }

    private async Task<(Patient Patient, Doctor OwnDoctor, Doctor OtherDoctor, DentalService Service)>
        SeedEntitiesAsync()
    {
        var patient = Patient.Create("Sara", "Habib",
            ContactInfo.Create("+963911111111", null, true, null).Value,
            DateTime.UtcNow.AddYears(-40), Gender.Female).Value;
        var ownDoctor = Doctor.Create("Omar", "Ali", "Orthodontics",
            ContactInfo.Create("+963922222222", null, true, null).Value, Gender.Male).Value;
        var otherDoctor = Doctor.Create("Lina", "Faris", "Orthodontics",
            ContactInfo.Create("+963933333333", null, true, null).Value, Gender.Female).Value;
        var service = DentalService.Create("Teeth Cleaning", "A routine dental cleaning procedure.", 30).Value;

        await _context.Patients.AddAsync(patient);
        await _context.Doctors.AddRangeAsync(ownDoctor, otherDoctor);
        await _context.DentalServices.AddAsync(service);
        await _context.SaveChangesAsync(default);

        return (patient, ownDoctor, otherDoctor, service);
    }
}
