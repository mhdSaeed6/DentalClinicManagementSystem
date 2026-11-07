using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.Reports.Queries.GetDashboardSummary;
using DentalClinic.Domain.Appointments;
using DentalClinic.Domain.Clinics;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Doctors;
using DentalClinic.Domain.Invoices;
using DentalClinic.Domain.Invoices.Enums;
using DentalClinic.Domain.Patients;
using DentalClinic.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

using Xunit;

namespace DentalClinic.Application.UnitTests.Queries;

/// <summary>
/// The dashboard used to aggregate five "page of 100" responses in the browser,
/// so every figure silently stopped at 100 rows. These tests pin the replacement:
/// SQL aggregates over the whole scoped set, with money split into the three
/// questions an owner actually asks (billed in the window, cash in the window,
/// owed on the books).
/// </summary>
public class GetDashboardSummaryQueryHandlerTests
{
    /// <summary>
    /// A fixed clinic clock. It sits in 2030 for two reasons: the window is then
    /// deterministic regardless of when the suite runs, and the domain's
    /// "an appointment must be in the future" rule (which reads the real clock)
    /// still holds for years.
    /// </summary>
    private static readonly DateTimeOffset Now = new(2030, 6, 15, 10, 0, 0, TimeSpan.Zero);

    /// <summary>Today, in the clinic's (UTC) calendar, after <see cref="Now"/>.</summary>
    private static readonly DateTime TodayLater = new(2030, 6, 15, 11, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_CountsEveryPatient_NotJustOnePage()
    {
        // The old dashboard showed the length of one page of 100, so patient
        // 101 and beyond never appeared in the total.
        await using var context = NewContext();
        await SeedClinicAsync(context);

        for (var i = 0; i < 120; i++)
        {
            context.Patients.Add(NewPatient($"Patient{i}", "Load", diabetic: i < 3));
        }

        await context.SaveChangesAsync();

        var result = await NewHandler(context).Handle(Unrestricted(), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(120, result.Value.TotalPatients);
        Assert.Equal(3, result.Value.PatientsWithMedicalAlerts);
    }

    [Fact]
    public async Task Handle_RestrictedDoctor_SeesOnlyTheirOwnFigures()
    {
        await using var context = NewContext();
        await SeedClinicAsync(context);

        var doctorA = NewDoctor("Alice", "Scope");
        var doctorB = NewDoctor("Bob", "Scope");
        var patientA = NewPatient("Anna", "Scope", createdBy: "user-a");
        var patientB = NewPatient("Ben", "Scope", createdBy: "user-b");

        context.Doctors.AddRange(doctorA, doctorB);
        context.Patients.AddRange(patientA, patientB);
        context.Appointments.AddRange(
            NewAppointment(patientA.Id, doctorA.Id, TodayLater),
            NewAppointment(patientB.Id, doctorB.Id, TodayLater.AddHours(1)));

        await context.SaveChangesAsync();

        var doctorScope = new DoctorAccessScope(true, doctorA.Id);
        var patientScope = new PatientAccessScope(true, doctorA.Id, "user-a");

        var restricted = await NewHandler(context).Handle(
            new GetDashboardSummaryQuery(doctorScope, patientScope),
            default);

        Assert.True(restricted.IsSuccess);
        Assert.Equal(1, restricted.Value.TotalPatients);
        Assert.Equal(1, restricted.Value.TodayAppointmentCount);
        var row = Assert.Single(restricted.Value.DoctorProductivity);
        Assert.Equal(doctorA.Id, row.DoctorId);
        Assert.Equal(1, row.AppointmentCount);

        // And the same data, unscoped, proves the filter actually narrowed it.
        var unrestricted = await NewHandler(context).Handle(Unrestricted(), default);

        Assert.Equal(2, unrestricted.Value.TotalPatients);
        Assert.Equal(2, unrestricted.Value.TodayAppointmentCount);
        Assert.Equal(2, unrestricted.Value.DoctorProductivity.Count);
    }

    [Fact]
    public async Task Handle_Revenue_CountsOnlyInvoicesIssuedInsideTheWindow()
    {
        await using var context = NewContext();
        await SeedClinicAsync(context);

        var patient = NewPatient("Rita", "Revenue");
        context.Patients.Add(patient);

        // Inside the default rolling 30 days, outside it, and a draft (an
        // estimate, not a bill).
        context.Invoices.Add(NewInvoice(patient.Id, 100m, new DateTimeOffset(2030, 6, 10, 8, 0, 0, TimeSpan.Zero)));
        context.Invoices.Add(NewInvoice(patient.Id, 500m, new DateTimeOffset(2030, 4, 1, 8, 0, 0, TimeSpan.Zero)));
        context.Invoices.Add(NewDraft(patient.Id, 300m));

        await context.SaveChangesAsync();

        var result = await NewHandler(context).Handle(Unrestricted(), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Revenue.TotalInvoiceCount);
        Assert.Equal(100m, result.Value.Revenue.TotalBilled);
        Assert.Equal(new DateOnly(2030, 5, 17), result.Value.From);
        Assert.Equal(new DateOnly(2030, 6, 15), result.Value.To);
    }

    [Fact]
    public async Task Handle_CashCollected_IsPaymentsInTheWindow_NotBilling()
    {
        await using var context = NewContext();
        await SeedClinicAsync(context);

        var patient = NewPatient("Cara", "Cash");
        context.Patients.Add(patient);

        var billed = NewInvoice(patient.Id, 200m, new DateTimeOffset(2030, 6, 12, 8, 0, 0, TimeSpan.Zero));
        context.Invoices.Add(billed);
        await context.SaveChangesAsync();

        billed.RegisterPayment(50m);
        var payment = Payment.Create(billed.Id, 50m).Value;
        context.Payments.Add(payment);
        await context.SaveChangesAsync();

        // Pin the receipt instant: the domain stamps UtcNow, which the fixed
        // clock in this test does not control.
        context.Entry(payment).Property(row => row.PaidAtUtc).CurrentValue =
            new DateTime(2030, 6, 15, 9, 0, 0, DateTimeKind.Utc);
        await context.SaveChangesAsync();

        var result = await NewHandler(context).Handle(Unrestricted(), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(50m, result.Value.CashCollected);
        Assert.Equal(200m, result.Value.Revenue.TotalBilled);
        Assert.Equal(50m, result.Value.Revenue.TotalPaid);
        Assert.Equal(150m, result.Value.Revenue.TotalOutstanding);
    }

    [Fact]
    public async Task Handle_Outstanding_IsNotScopedToTheWindow()
    {
        // Money owed since June is still owed in November; ageing it out of the
        // window would flatter the books.
        await using var context = NewContext();
        await SeedClinicAsync(context);

        var patient = NewPatient("Otto", "Owed");
        context.Patients.Add(patient);
        context.Invoices.Add(NewInvoice(patient.Id, 400m, new DateTimeOffset(2030, 1, 1, 8, 0, 0, TimeSpan.Zero)));
        await context.SaveChangesAsync();

        var result = await NewHandler(context).Handle(Unrestricted(), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.Revenue.TotalInvoiceCount);
        Assert.Equal(400m, result.Value.Outstanding.TotalOutstanding);
        Assert.Equal(1, result.Value.Outstanding.OpenInvoiceCount);
    }

    [Fact]
    public async Task Handle_Productivity_ExcludesStandaloneInvoices()
    {
        // An invoice with no appointment has no owning doctor. Attributing it to
        // the viewer would invent productivity, so it stays out of every row.
        await using var context = NewContext();
        await SeedClinicAsync(context);

        var doctor = NewDoctor("Dana", "Productive");
        var patient = NewPatient("Pia", "Productive");
        context.Doctors.Add(doctor);
        context.Patients.Add(patient);

        var appointment = NewAppointment(patient.Id, doctor.Id, TodayLater);
        context.Appointments.Add(appointment);

        context.Invoices.Add(NewInvoice(
            patient.Id,
            100m,
            new DateTimeOffset(2030, 6, 10, 8, 0, 0, TimeSpan.Zero),
            appointmentId: appointment.Id));
        context.Invoices.Add(NewInvoice(patient.Id, 900m, new DateTimeOffset(2030, 6, 12, 8, 0, 0, TimeSpan.Zero)));

        await context.SaveChangesAsync();

        var result = await NewHandler(context).Handle(Unrestricted(), default);

        Assert.True(result.IsSuccess);

        // Both invoices are billed in the window; only one has a doctor.
        Assert.Equal(1000m, result.Value.Revenue.TotalBilled);

        var row = Assert.Single(result.Value.DoctorProductivity);
        Assert.Equal(doctor.Id, row.DoctorId);
        Assert.Equal(1, row.IssuedInvoiceCount);
        Assert.Equal(100m, row.BilledAmount);
    }

    [Fact]
    public async Task Handle_WithAReversedRange_ReturnsTheInvalidRangeError()
    {
        await using var context = NewContext();
        await SeedClinicAsync(context);

        var result = await NewHandler(context).Handle(
            new GetDashboardSummaryQuery(
                DoctorAccessScope.None,
                PatientAccessScope.None,
                From: new DateOnly(2030, 6, 15),
                To: new DateOnly(2030, 6, 1)),
            default);

        Assert.True(result.IsError);
        Assert.Equal(ApplicationErrors.InvalidReportRange.Code, result.TopError.Code);
    }

    [Fact]
    public async Task Handle_WithAnExplicitRange_UsesItInsteadOfTheDefault()
    {
        await using var context = NewContext();
        await SeedClinicAsync(context);

        var patient = NewPatient("Wanda", "Window");
        context.Patients.Add(patient);
        context.Invoices.Add(NewInvoice(patient.Id, 700m, new DateTimeOffset(2030, 4, 1, 8, 0, 0, TimeSpan.Zero)));
        await context.SaveChangesAsync();

        var result = await NewHandler(context).Handle(
            new GetDashboardSummaryQuery(
                DoctorAccessScope.None,
                PatientAccessScope.None,
                From: new DateOnly(2030, 3, 1),
                To: new DateOnly(2030, 4, 30)),
            default);

        Assert.True(result.IsSuccess);
        Assert.Equal(new DateOnly(2030, 3, 1), result.Value.From);
        Assert.Equal(new DateOnly(2030, 4, 30), result.Value.To);
        Assert.Equal(700m, result.Value.Revenue.TotalBilled);
    }

    private static GetDashboardSummaryQuery Unrestricted() =>
        new(DoctorAccessScope.None, PatientAccessScope.None);

    private static GetDashboardSummaryQueryHandler NewHandler(AppDbContext context) =>
        new(context, new FixedTimeProvider(Now));

    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"dashboard-{Guid.NewGuid()}")
            .Options);

    /// <summary>
    /// The clinic profile pins the timezone to UTC so the day buckets in these
    /// tests are the same on every machine.
    /// </summary>
    private static async Task SeedClinicAsync(AppDbContext context)
    {
        var setting = ClinicSetting.Create(
            name: "Test Clinic",
            nameArabic: "عيادة الاختبار",
            logoUrl: null,
            phone: null,
            address: null,
            email: null,
            timeZoneId: "UTC",
            baseCurrency: "USD",
            secondaryCurrency: null,
            defaultCountryCode: null).Value;

        context.ClinicSettings.Add(setting);
        await context.SaveChangesAsync();
    }

    private static Patient NewPatient(string firstName, string lastName, string? createdBy = null, bool diabetic = false)
    {
        var patient = Patient.Create(
            firstName,
            lastName,
            ContactInfo.Create("01000000000").Value,
            new DateTime(1990, 1, 1),
            Gender.Male,
            diabetic ? new MedicalHistory { HasDiabetes = true } : null).Value;

        patient.CreatedBy = createdBy;
        return patient;
    }

    private static Doctor NewDoctor(string firstName, string lastName) =>
        Doctor.Create(
            firstName,
            lastName,
            "Orthodontics",
            ContactInfo.Create("01000000001").Value,
            Gender.Female).Value;

    private static Appointment NewAppointment(Guid patientId, Guid doctorId, DateTime scheduled) =>
        Appointment.Create(patientId, doctorId, serviceId: null, scheduled, 30, notes: null).Value;

    private static Invoice NewInvoice(
        Guid patientId,
        decimal totalAmount,
        DateTimeOffset issuedAt,
        Guid? appointmentId = null)
    {
        // The domain caps an invoice number at 32 characters; a shortened guid
        // is unique enough for a test and still valid.
        var number = $"INV-{Guid.NewGuid():N}"[..20];

        return Invoice.Create(
            patientId,
            totalAmount,
            appointmentId,
            InvoiceLifecycleStatus.Issued,
            number,
            issuedAt).Value;
    }

    private static Invoice NewDraft(Guid patientId, decimal totalAmount) =>
        Invoice.Create(patientId, totalAmount, lifecycleStatus: InvoiceLifecycleStatus.Draft).Value;

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}