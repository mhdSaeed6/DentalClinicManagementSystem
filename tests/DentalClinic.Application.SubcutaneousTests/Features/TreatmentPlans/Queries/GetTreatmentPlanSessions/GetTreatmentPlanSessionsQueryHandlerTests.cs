using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.TreatmentPlans.Queries.GetTreatmentPlanSessions;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Doctors;
using DentalClinic.Domain.Patients;
using DentalClinic.Domain.TreatmentPlans;
using DentalClinic.Domain.TreatmentRecords;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.TreatmentPlans.Queries.GetTreatmentPlanSessions;

[Collection(WebAppFactoryCollection.CollectionName)]
public class GetTreatmentPlanSessionsQueryHandlerTests(WebAppFactory factory) : IDisposable
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    // The shared TestCurrentUser is mutated by the role-switching tests and
    // must be reset to Admin so the following tests keep their default caller.
    public void Dispose() => factory.CurrentUser.AsAdmin();

    [Fact]
    public async Task Handle_ShouldGroupBySessionWithMixedStatuses()
    {
        var seed = await SeedPlanWithSessionsAsync();

        var result = await _mediator.Send(new GetTreatmentPlanSessionsQuery(
            Scope: DoctorAccessScope.None,
            PlanId: seed.Plan.Id));

        Assert.True(result.IsSuccess);
        var sessions = result.Value;

        Assert.Equal(2, sessions.Count);
        Assert.Equal(1, sessions[0].SessionNumber);
        Assert.Equal(3, sessions[0].ItemCount); // includes the cancelled one
        Assert.Equal(100m, sessions[0].EstimatedTotal);
        Assert.Equal(250m, sessions[0].PerformedTotal);
        Assert.Equal(350m, sessions[0].GrandTotal);

        Assert.Equal(2, sessions[1].SessionNumber);
        Assert.Equal(1, sessions[1].ItemCount);
        Assert.Equal(60m, sessions[1].EstimatedTotal);
        Assert.Equal(0m, sessions[1].PerformedTotal);
        Assert.Equal(60m, sessions[1].GrandTotal);
    }

    [Fact]
    public async Task Handle_PlanWithoutItems_ShouldReturnEmptyList()
    {
        var plan = await SeedEmptyPlanAsync();

        var result = await _mediator.Send(new GetTreatmentPlanSessionsQuery(
            Scope: DoctorAccessScope.None,
            PlanId: plan.Id));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Handle_UnknownPlan_ShouldReturnNotFound()
    {
        var result = await _mediator.Send(new GetTreatmentPlanSessionsQuery(
            Scope: DoctorAccessScope.None,
            PlanId: Guid.NewGuid()));

        Assert.True(result.IsError);
        Assert.Equal("TreatmentPlan.NotFound", result.TopError.Code);
    }

    // A restricted doctor only reads sessions of their own plans.
    [Fact]
    public async Task Handle_PlanOfAnotherDoctor_ShouldBeForbidden()
    {
        var seed = await SeedPlanWithSessionsAsync();

        factory.CurrentUser.AsDoctor(Guid.NewGuid()); // owns nothing

        var result = await _mediator.Send(new GetTreatmentPlanSessionsQuery(
            Scope: factory.CurrentUser.DoctorScope(),
            PlanId: seed.Plan.Id));

        Assert.True(result.IsError);
        Assert.Equal("TreatmentPlan.AccessForbidden", result.TopError.Code);

        factory.CurrentUser.AsDoctor(seed.Doctor.Id);
        var ownerResult = await _mediator.Send(new GetTreatmentPlanSessionsQuery(
            Scope: factory.CurrentUser.DoctorScope(),
            PlanId: seed.Plan.Id));

        Assert.True(ownerResult.IsSuccess);
        Assert.Equal(2, ownerResult.Value.Count);
    }

    private async Task<(TreatmentPlan Plan, Doctor Doctor)> SeedPlanWithSessionsAsync()
    {
        var contactInfo = ContactInfo.Create("+963911111111", null, true, null).Value;
        var patient = Patient.Create("Ahmad", "Kareem", contactInfo, DateTime.UtcNow.AddYears(-25), Domain.Common.Enums.Gender.Male).Value;
        var doctor = Doctor.Create("Omar", "Ali", "Endodontics", contactInfo, Domain.Common.Enums.Gender.Male).Value;
        var plan = TreatmentPlan.Create(patient.Id, doctor.Id, "Sessions plan").Value;

        var planned = TreatmentRecord.CreatePlanItem(plan.Id, patient.Id, doctor.Id, 26, "Root canal", 100m).Value;
        var performed = TreatmentRecord.CreatePlanItem(plan.Id, patient.Id, doctor.Id, 15, "Crown", 300m).Value;
        Assert.True(performed.MarkPerformed(250m).IsSuccess);

        var cancelled = TreatmentRecord.CreatePlanItem(plan.Id, patient.Id, doctor.Id, 27, "Extraction", 90m).Value;
        Assert.True(cancelled.MarkPerformed(90m).IsSuccess);
        Assert.True(cancelled.Cancel().IsSuccess); // stays in session 1 counts, out of money totals

        var secondSession = TreatmentRecord.CreatePlanItem(plan.Id, patient.Id, doctor.Id, 27, "Filling", 60m, sessionNumber: 2).Value;

        await _context.Patients.AddAsync(patient);
        await _context.Doctors.AddAsync(doctor);
        await _context.TreatmentPlans.AddAsync(plan);
        await _context.TreatmentRecords.AddAsync(planned);
        await _context.TreatmentRecords.AddAsync(performed);
        await _context.TreatmentRecords.AddAsync(cancelled);
        await _context.TreatmentRecords.AddAsync(secondSession);
        await _context.SaveChangesAsync(default);

        return (plan, doctor);
    }

    private async Task<TreatmentPlan> SeedEmptyPlanAsync()
    {
        var contactInfo = ContactInfo.Create("+963911111111", null, true, null).Value;
        var patient = Patient.Create("Lina", "Saeed", contactInfo, DateTime.UtcNow.AddYears(-30), Domain.Common.Enums.Gender.Female).Value;
        var doctor = Doctor.Create("Omar", "Ali", "Endodontics", contactInfo, Domain.Common.Enums.Gender.Male).Value;
        var plan = TreatmentPlan.Create(patient.Id, doctor.Id, "Empty plan").Value;

        await _context.Patients.AddAsync(patient);
        await _context.Doctors.AddAsync(doctor);
        await _context.TreatmentPlans.AddAsync(plan);
        await _context.SaveChangesAsync(default);

        return plan;
    }
}