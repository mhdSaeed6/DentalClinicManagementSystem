using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.TreatmentPlans.Commands.RemoveTreatmentPlan;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Doctors;
using DentalClinic.Domain.Patients;
using DentalClinic.Domain.TreatmentPlans;
using DentalClinic.Domain.TreatmentRecords;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.TreatmentPlans.Commands.RemoveTreatmentPlan;

[Collection(WebAppFactoryCollection.CollectionName)]
public class RemoveTreatmentPlanCommandHandlerTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    // D-P5: soft delete only — performed (potentially billed) work must never
    // disappear without an explicit confirmation.
    [Fact]
    public async Task Handle_PerformedItemsWithoutConfirmation_ShouldReturnConflict()
    {
        var plan = await SeedPlanWithPerformedItemAsync();

        var result = await _mediator.Send(new RemoveTreatmentPlanCommand(plan.Id));

        Assert.True(result.IsError);
        Assert.Equal("TreatmentPlan.PlanHasPerformedItems", result.TopError.Code);

        var dbPlan = await _context.TreatmentPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == plan.Id);
        Assert.NotNull(dbPlan); // nothing was deleted silently
    }

    [Fact]
    public async Task Handle_PerformedItemsWithConfirmation_ShouldSoftDelete()
    {
        var plan = await SeedPlanWithPerformedItemAsync();

        var result = await _mediator.Send(new RemoveTreatmentPlanCommand(plan.Id, ConfirmPerformedItems: true));

        Assert.True(result.IsSuccess);

        // The row survives with IsDeleted (soft delete — checked through the
        // ignore-filters view); the default filtered view no longer returns it.
        var deletedRow = await _context.TreatmentPlans
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == plan.Id);
        Assert.NotNull(deletedRow);
        Assert.True(deletedRow!.IsDeleted);

        var visiblePlan = await _context.TreatmentPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == plan.Id);
        Assert.Null(visiblePlan);
    }

    [Fact]
    public async Task Handle_OnlyPlannedItemsWithoutConfirmation_ShouldSoftDelete()
    {
        var plan = await SeedPlanWithPlannedItemOnlyAsync();

        var result = await _mediator.Send(new RemoveTreatmentPlanCommand(plan.Id));

        Assert.True(result.IsSuccess);

        var visiblePlan = await _context.TreatmentPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == plan.Id);
        Assert.Null(visiblePlan);
    }

    [Fact]
    public async Task Handle_UnknownPlan_ShouldReturnNotFound()
    {
        var result = await _mediator.Send(new RemoveTreatmentPlanCommand(Guid.NewGuid()));

        Assert.True(result.IsError);
        Assert.Equal("TreatmentPlan.NotFound", result.TopError.Code);
    }

    // Seeds a plan holding one performed item (cost 250) so any delete attempt
    // hits the D-P5 confirmation gate.
    private async Task<TreatmentPlan> SeedPlanWithPerformedItemAsync()
    {
        var contactInfo = ContactInfo.Create("+963911111111", null, true, null).Value;
        var patient = Patient.Create("Ahmad", "Kareem", contactInfo, DateTime.UtcNow.AddYears(-25), Domain.Common.Enums.Gender.Male).Value;
        var doctor = Doctor.Create("Omar", "Ali", "Periodontics", contactInfo, Domain.Common.Enums.Gender.Male).Value;
        var plan = TreatmentPlan.Create(patient.Id, doctor.Id, "Plan with performed work").Value;
        var item = TreatmentRecord.CreatePlanItem(plan.Id, patient.Id, doctor.Id, 36, "Crown fitting", 300m).Value;
        Assert.True(item.MarkPerformed(250m).IsSuccess);

        await _context.Patients.AddAsync(patient);
        await _context.Doctors.AddAsync(doctor);
        await _context.TreatmentPlans.AddAsync(plan);
        await _context.TreatmentRecords.AddAsync(item);
        await _context.SaveChangesAsync(default);

        return plan;
    }

    private async Task<TreatmentPlan> SeedPlanWithPlannedItemOnlyAsync()
    {
        var contactInfo = ContactInfo.Create("+963911111111", null, true, null).Value;
        var patient = Patient.Create("Lina", "Saeed", contactInfo, DateTime.UtcNow.AddYears(-30), Domain.Common.Enums.Gender.Female).Value;
        var doctor = Doctor.Create("Omar", "Ali", "Periodontics", contactInfo, Domain.Common.Enums.Gender.Male).Value;
        var plan = TreatmentPlan.Create(patient.Id, doctor.Id, "Plan without performed work").Value;
        var item = TreatmentRecord.CreatePlanItem(plan.Id, patient.Id, doctor.Id, 46, "Implant", 400m).Value;

        await _context.Patients.AddAsync(patient);
        await _context.Doctors.AddAsync(doctor);
        await _context.TreatmentPlans.AddAsync(plan);
        await _context.TreatmentRecords.AddAsync(item);
        await _context.SaveChangesAsync(default);

        return plan;
    }
}