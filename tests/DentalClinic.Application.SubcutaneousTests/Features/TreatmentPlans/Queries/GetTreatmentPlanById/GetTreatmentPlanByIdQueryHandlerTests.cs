using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.TreatmentPlans.Queries.GetTreatmentPlanById;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Doctors;
using DentalClinic.Domain.Patients;
using DentalClinic.Domain.TreatmentPlans;
using DentalClinic.Domain.TreatmentRecords;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.TreatmentPlans.Queries.GetTreatmentPlanById;

[Collection(WebAppFactoryCollection.CollectionName)]
public class GetTreatmentPlanByIdQueryHandlerTests(WebAppFactory factory) : IDisposable
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    // The shared TestCurrentUser is mutated by the role-switching tests and
    // must be reset to Admin so the following tests keep their default caller.
    public void Dispose() => factory.CurrentUser.AsAdmin();

    [Fact]
    public async Task Handle_ShouldReturnDetailWithItemsAndTotals()
    {
        var seed = await SeedPlanWithMixedItemsAsync();

        var query = new GetTreatmentPlanByIdQuery(
            Scope: DoctorAccessScope.None,
            PlanId: seed.Plan.Id);

        var result = await _mediator.Send(query);

        Assert.True(result.IsSuccess);
        var detail = result.Value;
        Assert.Equal(seed.Plan.Id, detail.Id);
        Assert.Equal(4, detail.ItemCount);
        Assert.Equal(160m, detail.EstimatedTotal); // 100 + 60 planned estimates
        Assert.Equal(250m, detail.PerformedTotal);
        Assert.Equal(4, detail.Items.Count);

        // Items order: session ascending then creation — sessions must never mix.
        Assert.Equal([1, 1, 1, 2], detail.Items.Select(i => i.SessionNumber).ToArray());
        Assert.Equal(0m, detail.Items.Single(i => i.Status == TreatmentRecordStatus.Planned && i.SessionNumber == 2).Cost);
        Assert.Equal("Ahmad Kareem", detail.PatientName);
        Assert.Equal("Omar Ali", detail.DoctorName);
    }

    [Fact]
    public async Task Handle_UnknownPlan_ShouldReturnNotFound()
    {
        var result = await _mediator.Send(new GetTreatmentPlanByIdQuery(
            Scope: DoctorAccessScope.None,
            PlanId: Guid.NewGuid()));

        Assert.True(result.IsError);
        Assert.Equal("TreatmentPlan.NotFound", result.TopError.Code);
    }

    // A restricted doctor only reads plans from their own profile (fail-closed
    // — forbidden, never silently emptied).
    [Fact]
    public async Task Handle_PlanOfAnotherDoctor_ShouldBeForbidden()
    {
        var seed = await SeedPlanWithMixedItemsAsync();

        factory.CurrentUser.AsDoctor(Guid.NewGuid()); // owns nothing

        var query = new GetTreatmentPlanByIdQuery(
            Scope: factory.CurrentUser.DoctorScope(),
            PlanId: seed.Plan.Id);
        var result = await _mediator.Send(query);

        Assert.True(result.IsError);
        Assert.Equal("TreatmentPlan.AccessForbidden", result.TopError.Code);

        // The owning doctor reads it fine.
        factory.CurrentUser.AsDoctor(seed.Doctor.Id);
        var ownerQuery = new GetTreatmentPlanByIdQuery(
            Scope: factory.CurrentUser.DoctorScope(),
            PlanId: seed.Plan.Id);
        var ownerResult = await _mediator.Send(ownerQuery);

        Assert.True(ownerResult.IsSuccess);
        Assert.Equal(4, ownerResult.Value.Items.Count);
    }

    private async Task<(TreatmentPlan Plan, Patient Patient, Doctor Doctor)> SeedPlanWithMixedItemsAsync()
    {
        var contactInfo = ContactInfo.Create("+963911111111", null, true, null).Value;
        var patient = Patient.Create("Ahmad", "Kareem", contactInfo, DateTime.UtcNow.AddYears(-25), Domain.Common.Enums.Gender.Male).Value;
        var doctor = Doctor.Create("Omar", "Ali", "Endodontics", contactInfo, Domain.Common.Enums.Gender.Male).Value;
        var plan = TreatmentPlan.Create(patient.Id, doctor.Id, "Detail plan").Value;

        var firstPlanned = TreatmentRecord.CreatePlanItem(plan.Id, patient.Id, doctor.Id, 26, "Root canal", 100m).Value;
        var secondPlanned = TreatmentRecord.CreatePlanItem(plan.Id, patient.Id, doctor.Id, 27, "Filling", 60m, sessionNumber: 2).Value;
        var performed = TreatmentRecord.CreatePlanItem(plan.Id, patient.Id, doctor.Id, 15, "Crown", 300m).Value;
        Assert.True(performed.MarkPerformed(250m).IsSuccess);
        var cancelled = TreatmentRecord.CreatePlanItem(plan.Id, patient.Id, doctor.Id, 35, "Extraction", 90m).Value;
        Assert.True(cancelled.MarkPerformed(90m).IsSuccess);
        Assert.True(cancelled.Cancel().IsSuccess);

        await _context.Patients.AddAsync(patient);
        await _context.Doctors.AddAsync(doctor);
        await _context.TreatmentPlans.AddAsync(plan);
        await _context.TreatmentRecords.AddAsync(firstPlanned);
        await _context.TreatmentRecords.AddAsync(secondPlanned);
        await _context.TreatmentRecords.AddAsync(performed);
        await _context.TreatmentRecords.AddAsync(cancelled);
        await _context.SaveChangesAsync(default);

        return (plan, patient, doctor);
    }
}