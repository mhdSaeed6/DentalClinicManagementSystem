using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.TreatmentPlans.Commands.CancelTreatmentPlanItem;
using DentalClinic.Application.Features.TreatmentPlans.Commands.PerformTreatmentPlanItem;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Appointments;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Doctors;
using DentalClinic.Domain.Patients;
using DentalClinic.Domain.TreatmentPlans;
using DentalClinic.Domain.TreatmentRecords;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.TreatmentPlans.Commands.PerformTreatmentPlanItem;

[Collection(WebAppFactoryCollection.CollectionName)]
public class PerformTreatmentPlanItemCommandHandlerTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    [Fact]
    public async Task Handle_WithValidActualCost_ShouldPerformAndBecomeBillable()
    {
        // Arrange
        var (_, item) = await SeedPlanWithItemAsync();

        // Act: the actual charged cost replaces the silent estimate (D-P2).
        var result = await _mediator.Send(new PerformTreatmentPlanItemCommand(item.PlanId!.Value, item.Id, 250m));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(TreatmentRecordStatus.Performed, result.Value.Status);
        Assert.Equal(250m, result.Value.Cost);
        Assert.Equal(300m, result.Value.EstimatedCost); // التقدير يبقى في التاريخ للطباعة

        var dbItem = await _context.TreatmentRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == item.Id);

        Assert.NotNull(dbItem);
        Assert.True(dbItem.IsBillable);
        Assert.Equal(250m, dbItem.Cost);
    }

    [Fact]
    public async Task Handle_Twice_ShouldReturnConflict()
    {
        var (_, item) = await SeedPlanWithItemAsync();

        var first = await _mediator.Send(new PerformTreatmentPlanItemCommand(item.PlanId!.Value, item.Id, 250m));
        Assert.True(first.IsSuccess);

        var second = await _mediator.Send(new PerformTreatmentPlanItemCommand(item.PlanId!.Value, item.Id, 250m));

        Assert.True(second.IsError);
        Assert.Equal(TreatmentRecordErrors.ItemAlreadyPerformed.Code, second.TopError.Code);
    }

    [Fact]
    public async Task Handle_CancelledItem_ShouldReturnConflict()
    {
        var (_, item) = await SeedPlanWithItemAsync();
        var cancel = await _mediator.Send(new CancelTreatmentPlanItemCommand(item.PlanId!.Value, item.Id));
        Assert.True(cancel.IsSuccess);

        var result = await _mediator.Send(new PerformTreatmentPlanItemCommand(item.PlanId!.Value, item.Id, 250m));

        Assert.True(result.IsError);
        Assert.Equal(TreatmentRecordErrors.ItemCancelled.Code, result.TopError.Code);
    }

    [Fact]
    public async Task Handle_WithUnknownAppointment_ShouldReturnAppointmentNotFound()
    {
        var (_, item) = await SeedPlanWithItemAsync();

        var result = await _mediator.Send(new PerformTreatmentPlanItemCommand(
            item.PlanId!.Value, item.Id, 250m, AppointmentId: Guid.NewGuid()));

        Assert.True(result.IsError);
        Assert.Equal(ApplicationErrors.AppointmentNotFound.Code, result.TopError.Code);
    }

    [Fact]
    public async Task Handle_WithAnotherPatientsAppointment_ShouldReturnMismatch()
    {
        var (_, item) = await SeedPlanWithItemAsync();
        var otherContact = ContactInfo.Create("+963922222222", null, true, null).Value;
        var otherPatient = Patient.Create(
            "Sara",
            "Noor",
            otherContact,
            DateTime.UtcNow.AddYears(-30),
            Domain.Common.Enums.Gender.Female).Value;
        var appointment = Appointment.Create(
            otherPatient.Id,
            item.DoctorId,
            null,
            DateTime.UtcNow.AddDays(1),
            30).Value;

        await _context.Patients.AddAsync(otherPatient);
        await _context.Appointments.AddAsync(appointment);
        await _context.SaveChangesAsync(default);

        var result = await _mediator.Send(new PerformTreatmentPlanItemCommand(
            item.PlanId!.Value, item.Id, 250m, AppointmentId: appointment.Id));

        Assert.True(result.IsError);
        Assert.Equal(ApplicationErrors.TreatmentPlanItemAppointmentMismatch.Code, result.TopError.Code);
    }

    // Seeds a plan with one planned item (estimate 300, session 1).
    private async Task<(TreatmentPlan Plan, TreatmentRecord Item)> SeedPlanWithItemAsync()
    {
        var contactInfo = ContactInfo.Create("+963911111111", null, true, null).Value;
        var patient = Patient.Create("Ahmad", "Kareem", contactInfo, DateTime.UtcNow.AddYears(-25), Domain.Common.Enums.Gender.Male).Value;
        var doctor = Doctor.Create("Omar", "Ali", "Endodontics", contactInfo, Domain.Common.Enums.Gender.Male).Value;
        var plan = TreatmentPlan.Create(patient.Id, doctor.Id, "Rehabilitation plan").Value;
        var item = TreatmentRecord.CreatePlanItem(plan.Id, patient.Id, doctor.Id, 26, "Root canal treatment", 300m).Value;

        await _context.Patients.AddAsync(patient);
        await _context.Doctors.AddAsync(doctor);
        await _context.TreatmentPlans.AddAsync(plan);
        await _context.TreatmentRecords.AddAsync(item);
        await _context.SaveChangesAsync(default);

        return (plan, item);
    }
}