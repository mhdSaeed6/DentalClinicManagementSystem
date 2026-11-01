using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Invoices.Commands.GenerateAppointmentInvoice;
using DentalClinic.Application.Features.Invoices.Commands.IssueInvoice;
using DentalClinic.Application.Features.TreatmentPlans.Commands.ReopenTreatmentPlanItem;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Appointments;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Doctors;
using DentalClinic.Domain.Invoices.Enums;
using DentalClinic.Domain.Patients;
using DentalClinic.Domain.TreatmentPlans;
using DentalClinic.Domain.TreatmentRecords;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.TreatmentPlans.Commands.ReopenTreatmentPlanItem;

[Collection(WebAppFactoryCollection.CollectionName)]
public class ReopenTreatmentPlanItemCommandHandlerTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    [Fact]
    public async Task Handle_PerformedItemWithoutInvoice_ShouldReturnToPlanned()
    {
        var (_, item, _) = await SeedPerformedPlanItemAsync();

        var result = await _mediator.Send(new ReopenTreatmentPlanItemCommand(item.PlanId!.Value, item.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal(TreatmentRecordStatus.Planned, result.Value.Status);
        Assert.Equal(300m, result.Value.EstimatedCost); // التقدير متاح للتنفيذ مرة ثانية

        var dbItem = await _context.TreatmentRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == item.Id);

        Assert.NotNull(dbItem);
        Assert.False(dbItem.IsBillable);
    }

    // D-P4: the "already on an issued invoice" guard. Invoice lines carry no
    // treatment id, so the rule is conservative — an item attached to an
    // appointment with an issued invoice can never silently reopen.
    [Fact]
    public async Task Handle_OnIssuedInvoice_ShouldBeBlocked()
    {
        var (_, item, appointment) = await SeedPerformedPlanItemAsync();

        var generated = await _mediator.Send(new GenerateAppointmentInvoiceCommand(item.AppointmentId!.Value));
        Assert.True(generated.IsSuccess);

        var issued = await _mediator.Send(new IssueInvoiceCommand(generated.Value.InvoiceId));
        Assert.True(issued.IsSuccess);

        // Sanity for the guard's premise — the invoice really is issued.
        var lifecycle = await _context.Invoices
            .AsNoTracking()
            .Where(i => i.Id == generated.Value.InvoiceId)
            .Select(i => i.LifecycleStatus)
            .FirstOrDefaultAsync();
        Assert.Equal(InvoiceLifecycleStatus.Issued, lifecycle);
        Assert.NotNull(appointment);

        var result = await _mediator.Send(new ReopenTreatmentPlanItemCommand(item.PlanId!.Value, item.Id));

        Assert.True(result.IsError);
        Assert.Equal(ApplicationErrors.TreatmentPlanItemOnIssuedInvoice.Code, result.TopError.Code);
    }

    [Fact]
    public async Task Handle_DraftOnlyInvoice_ShouldAllowReopen()
    {
        var (_, item, _) = await SeedPerformedPlanItemAsync();

        var generated = await _mediator.Send(new GenerateAppointmentInvoiceCommand(item.AppointmentId!.Value));
        Assert.True(generated.IsSuccess); // Draft — not issued yet

        var result = await _mediator.Send(new ReopenTreatmentPlanItemCommand(item.PlanId!.Value, item.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal(TreatmentRecordStatus.Planned, result.Value.Status);
    }

    // Seeds a completed appointment linked to one performed plan item that no
    // invoice covers yet — the D-P4 guard's happy-path premise.
    private async Task<(TreatmentPlan Plan, TreatmentRecord Item, Appointment Appointment)> SeedPerformedPlanItemAsync()
    {
        var contactInfo = ContactInfo.Create("+963911111111", null, true, null).Value;
        var patient = Patient.Create("Ahmad", "Kareem", contactInfo, DateTime.UtcNow.AddYears(-25), Domain.Common.Enums.Gender.Male).Value;
        var doctor = Doctor.Create("Omar", "Ali", "Endodontics", contactInfo, Domain.Common.Enums.Gender.Male).Value;
        var plan = TreatmentPlan.Create(patient.Id, doctor.Id, "Reopen flow plan").Value;
        var appointment = Appointment.Create(patient.Id, doctor.Id, null, DateTime.UtcNow.AddHours(1), 30).Value;
        appointment.Complete();

        var item = TreatmentRecord.CreatePlanItem(plan.Id, patient.Id, doctor.Id, 26, "Root canal treatment", 300m).Value;
        item.MarkPerformed(250m, appointment.Id);

        await _context.Patients.AddAsync(patient);
        await _context.Doctors.AddAsync(doctor);
        await _context.TreatmentPlans.AddAsync(plan);
        await _context.Appointments.AddAsync(appointment);
        await _context.TreatmentRecords.AddAsync(item);
        await _context.SaveChangesAsync(default);

        return (plan, item, appointment);
    }
}