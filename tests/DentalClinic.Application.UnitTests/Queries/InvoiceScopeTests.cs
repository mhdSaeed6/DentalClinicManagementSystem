using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.Invoices.Queries.GetInvoices;
using DentalClinic.Application.Features.Invoices.Queries.GetInvoiceSummary;
using DentalClinic.Application.Features.Invoices.Queries.GetOutstandingDebts;
using DentalClinic.Domain.Appointments;
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
/// The invoice list, the money summary and the debt report used to apply the
/// doctor scope with three hand-copied predicates. These tests pin the shared
/// rule: a doctor reaches the invoices of their own visits and nothing else.
/// </summary>
public class InvoiceScopeTests
{
    [Fact]
    public async Task GetInvoices_RestrictedDoctor_SeesOwnVisitInvoice_ButNotAStandaloneInvoice()
    {
        await using var context = NewContext();

        var (doctorA, _, ownInvoice, _) = await SeedAsync(context);

        var result = await new GetInvoicesQueryHandler(context)
            .Handle(new GetInvoicesQuery(Restricted(doctorA.Id)), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalCount);

        // Exactly their own visit: the other doctor's visit, the hand-made
        // invoice and the draft are all out of reach.
        var invoice = Assert.Single(result.Value.Items!);
        Assert.Equal(ownInvoice.Id, invoice.Id);
    }

    [Fact]
    public async Task GetInvoices_UnrestrictedStaff_SeesStandaloneAndVisitInvoices()
    {
        await using var context = NewContext();
        await SeedAsync(context);

        var result = await new GetInvoicesQueryHandler(context)
            .Handle(new GetInvoicesQuery(DoctorAccessScope.None), default);

        Assert.True(result.IsSuccess);

        // Every invoice, drafts included: the list is an operational screen, not
        // a money total, so nothing is hidden from staff who can see everything.
        Assert.Equal(4, result.Value.TotalCount);
    }

    [Fact]
    public async Task GetInvoiceSummary_RestrictedDoctor_CountsExactlyTheInvoicesTheListShows()
    {
        await using var context = NewContext();
        var (doctorA, _, ownInvoice, _) = await SeedAsync(context);

        var list = await new GetInvoicesQueryHandler(context)
            .Handle(new GetInvoicesQuery(Restricted(doctorA.Id)), default);

        var summary = await new GetInvoiceSummaryQueryHandler(context)
            .Handle(new GetInvoiceSummaryQuery(Restricted(doctorA.Id)), default);

        Assert.True(summary.IsSuccess);

        // The report and the billing screen must never disagree about the
        // caller's own money.
        Assert.Equal(list.Value.TotalCount, summary.Value.TotalInvoiceCount);
        Assert.Equal(ownInvoice.TotalAmount, summary.Value.TotalBilled);
    }

    [Fact]
    public async Task GetOutstandingDebts_RestrictedDoctor_IgnoresAStandaloneInvoice()
    {
        await using var context = NewContext();
        var (doctorA, _, ownInvoice, _) = await SeedAsync(context);

        var result = await new GetOutstandingDebtsQueryHandler(context)
            .Handle(new GetOutstandingDebtsQuery(Restricted(doctorA.Id)), default);

        Assert.True(result.IsSuccess);

        // One patient owes money on four invoices, but only the doctor's own
        // visit is theirs to chase.
        var debt = Assert.Single(result.Value.Items!);
        Assert.Equal(1, debt.OpenInvoiceCount);
        Assert.Equal(ownInvoice.TotalAmount, debt.TotalOutstanding);
    }

    private static DoctorAccessScope Restricted(Guid doctorId) => new(IsRestricted: true, DoctorId: doctorId);

    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"invoice-scope-{Guid.NewGuid()}")
            .Options);

    /// <summary>
    /// Seeds one patient with four invoices: doctor A's visit, doctor B's visit,
    /// an invoice with no visit at all, and a draft. Only doctor A's visit is
    /// reachable by doctor A.
    /// </summary>
    private static async Task<(Doctor DoctorA, Doctor DoctorB, Invoice OwnInvoice, Invoice StandaloneInvoice)>
        SeedAsync(AppDbContext context)
    {
        var doctorA = NewDoctor("Alice", "Scope");
        var doctorB = NewDoctor("Bob", "Scope");
        var patient = NewPatient();

        context.Doctors.AddRange(doctorA, doctorB);
        context.Patients.Add(patient);

        var visitA = NewAppointment(patient.Id, doctorA.Id);
        var visitB = NewAppointment(patient.Id, doctorB.Id);
        context.Appointments.AddRange(visitA, visitB);

        var ownInvoice = NewInvoice(patient.Id, 120m, visitA.Id);
        var foreignInvoice = NewInvoice(patient.Id, 700m, visitB.Id);
        var standaloneInvoice = NewInvoice(patient.Id, 500m, appointmentId: null);
        var draft = Invoice.Create(
            patient.Id,
            90m,
            lifecycleStatus: InvoiceLifecycleStatus.Draft).Value;

        context.Invoices.AddRange(ownInvoice, foreignInvoice, standaloneInvoice, draft);

        await context.SaveChangesAsync();

        return (doctorA, doctorB, ownInvoice, standaloneInvoice);
    }

    private static Patient NewPatient() =>
        Patient.Create(
            "Scoped",
            "Patient",
            ContactInfo.Create("01000000000").Value,
            new DateTime(1990, 1, 1),
            Gender.Male).Value;

    private static Doctor NewDoctor(string firstName, string lastName) =>
        Doctor.Create(
            firstName,
            lastName,
            "Orthodontics",
            ContactInfo.Create("01000000001").Value,
            Gender.Female).Value;

    private static Appointment NewAppointment(Guid patientId, Guid doctorId) =>
        Appointment.Create(
            patientId,
            doctorId,
            serviceId: null,
            DateTime.UtcNow.AddDays(1),
            30,
            notes: null).Value;

    private static Invoice NewInvoice(Guid patientId, decimal total, Guid? appointmentId)
    {
        // The domain caps an invoice number at 32 characters; a shortened guid
        // is unique enough for a test and still valid.
        var number = $"INV-{Guid.NewGuid():N}"[..20];

        return Invoice.Create(
            patientId,
            total,
            appointmentId,
            InvoiceLifecycleStatus.Issued,
            number,
            DateTimeOffset.UtcNow).Value;
    }
}