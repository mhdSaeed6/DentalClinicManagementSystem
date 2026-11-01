using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.TreatmentPlans.Queries.GetTreatmentPlans;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Doctors;
using DentalClinic.Domain.Patients;
using DentalClinic.Domain.TreatmentPlans;
using DentalClinic.Domain.TreatmentRecords;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.TreatmentPlans.Queries.GetTreatmentPlans;

[Collection(WebAppFactoryCollection.CollectionName)]
public class GetTreatmentPlansQueryHandlerTests(WebAppFactory factory) : IDisposable
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    // The shared TestCurrentUser is mutated by the role-switching tests and
    // must be reset to Admin so the following tests keep their default caller.
    public void Dispose() => factory.CurrentUser.AsAdmin();

    [Fact]
    public async Task Handle_ShouldReturnPlanWithMixedStatusTotals()
    {
        // Arrange: planned contributes its estimate, performed its charged cost,
        // cancelled contributes nothing — all in one plan.
        var seed = await SeedPlanWithMixedItemsAsync();

        var query = new GetTreatmentPlansQuery(
            Scope: DoctorAccessScope.None,
            PatientId: seed.Patient.Id);

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        var plan = Assert.Single(result.Value.Items!, p => p.Id == seed.Plan.Id);
        Assert.Equal(3, plan.ItemCount);
        Assert.Equal(100m, plan.EstimatedTotal); // planned estimate only
        Assert.Equal(250m, plan.PerformedTotal); // performed charge only
        Assert.Equal(TreatmentPlanStatus.Draft, plan.Status);
        Assert.Equal("Ahmad Kareem", plan.PatientName);
        Assert.Equal("Omar Ali", plan.DoctorName);
    }

    [Fact]
    public async Task Handle_WithPatientFilter_ShouldOnlyReturnThatPatientsPlans()
    {
        var (patientA, patientB) = await SeedTwoPatientsWithPlansAsync();

        var query = new GetTreatmentPlansQuery(
            Scope: DoctorAccessScope.None,
            PatientId: patientA.Patient.Id);

        var result = await _mediator.Send(query);

        Assert.True(result.IsSuccess);
        Assert.All(result.Value.Items!, p => Assert.Equal(patientA.Patient.Id, p.PatientId));
        Assert.Contains(result.Value.Items!, p => p.Id == patientA.PlanId);
        Assert.DoesNotContain(result.Value.Items!, p => p.Id == patientB.PlanId);
    }

    [Fact]
    public async Task Handle_WithStatusFilter_ShouldOnlyReturnMatchingPlans()
    {
        var (draftPlan, activePlan) = await SeedDraftAndActivePlansAsync();

        var draftQuery = new GetTreatmentPlansQuery(
            Scope: DoctorAccessScope.None,
            PatientId: draftPlan.Patient.Id,
            Status: TreatmentPlanStatus.Active);
        var draftResult = await _mediator.Send(draftQuery);

        // The draft plan is filtered out by the status predicate.
        Assert.True(draftResult.IsSuccess);
        Assert.DoesNotContain(draftResult.Value.Items!, p => p.Id == draftPlan.Plan.Id);

        var activeQuery = new GetTreatmentPlansQuery(
            Scope: DoctorAccessScope.None,
            PatientId: activePlan.Patient.Id,
            Status: TreatmentPlanStatus.Active);
        var activeResult = await _mediator.Send(activeQuery);

        Assert.True(activeResult.IsSuccess);
        Assert.Contains(activeResult.Value.Items!, p => p.Id == activePlan.Plan.Id);
    }

    // A restricted doctor scope must hide other doctors' plans entirely —
    // the scope travels in the query record, derived from the current user.
    [Fact]
    public async Task Handle_WithDoctorScope_ShouldHideOtherDoctorsPlans()
    {
        var seed = await SeedPlanForScopeTestAsync();

        factory.CurrentUser.AsDoctor(Guid.NewGuid()); // a caller that owns nothing

        var query = new GetTreatmentPlansQuery(
            Scope: factory.CurrentUser.DoctorScope(),
            PatientId: seed.Patient.Id);
        var result = await _mediator.Send(query);

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain(result.Value.Items!, p => p.Id == seed.Plan.Id);

        // Sanity: the very same filters from the owning doctor return the plan.
        factory.CurrentUser.AsDoctor(seed.Doctor.Id);
        var ownerQuery = new GetTreatmentPlansQuery(
            Scope: factory.CurrentUser.DoctorScope(),
            PatientId: seed.Patient.Id);
        var ownerResult = await _mediator.Send(ownerQuery);

        Assert.True(ownerResult.IsSuccess);
        Assert.Contains(ownerResult.Value.Items!, p => p.Id == seed.Plan.Id);
    }

    private async Task<(TreatmentPlan Plan, Patient Patient, Doctor Doctor)> SeedPlanWithMixedItemsAsync()
    {
        var contactInfo = ContactInfo.Create("+963911111111", null, true, null).Value;
        var patient = Patient.Create("Ahmad", "Kareem", contactInfo, DateTime.UtcNow.AddYears(-25), Domain.Common.Enums.Gender.Male).Value;
        var doctor = Doctor.Create("Omar", "Ali", "Endodontics", contactInfo, Domain.Common.Enums.Gender.Male).Value;
        var plan = TreatmentPlan.Create(patient.Id, doctor.Id, "Totals plan").Value;

        var planned = TreatmentRecord.CreatePlanItem(plan.Id, patient.Id, doctor.Id, 26, "Root canal", 100m).Value;
        var performed = TreatmentRecord.CreatePlanItem(plan.Id, patient.Id, doctor.Id, 15, "Crown", 300m, sessionNumber: 2).Value;
        Assert.True(performed.MarkPerformed(250m).IsSuccess);
        var cancelled = TreatmentRecord.CreatePlanItem(plan.Id, patient.Id, doctor.Id, 27, "Extraction", 90m).Value;
        Assert.True(cancelled.MarkPerformed(90m).IsSuccess);
        Assert.True(cancelled.Cancel().IsSuccess);

        await _context.Patients.AddAsync(patient);
        await _context.Doctors.AddAsync(doctor);
        await _context.TreatmentPlans.AddAsync(plan);
        await _context.TreatmentRecords.AddAsync(planned);
        await _context.TreatmentRecords.AddAsync(performed);
        await _context.TreatmentRecords.AddAsync(cancelled);
        await _context.SaveChangesAsync(default);

        return (plan, patient, doctor);
    }

    private async Task<((Patient Patient, Guid PlanId) PatientA, (Patient Patient, Guid PlanId) PatientB)>
        SeedTwoPatientsWithPlansAsync()
    {
        // Owned value objects: every patient needs its own ContactInfo instance —
        // two owners cannot share one owned entity.
        var contactInfoA = ContactInfo.Create("+963911111111", null, true, null).Value;
        var contactInfoB = ContactInfo.Create("+963922222222", null, true, null).Value;
        var doctorContactInfo = ContactInfo.Create("+963933333333", null, true, null).Value;
        var patientA = Patient.Create("Filter", "TesterA", contactInfoA, DateTime.UtcNow.AddYears(-40), Domain.Common.Enums.Gender.Male).Value;
        var patientB = Patient.Create("Filter", "TesterB", contactInfoB, DateTime.UtcNow.AddYears(-41), Domain.Common.Enums.Gender.Female).Value;
        var doctor = Doctor.Create("Omar", "Ali", "Oral Surgery", doctorContactInfo, Domain.Common.Enums.Gender.Male).Value;
        var planA = TreatmentPlan.Create(patientA.Id, doctor.Id, "Draft plan A").Value;
        var planB = TreatmentPlan.Create(patientB.Id, doctor.Id, "Active plan B").Value;
        Assert.True(planB.Activate().IsSuccess); // Draft stays Draft by construction

        await _context.Patients.AddAsync(patientA);
        await _context.Patients.AddAsync(patientB);
        await _context.Doctors.AddAsync(doctor);
        await _context.TreatmentPlans.AddAsync(planA);
        await _context.TreatmentPlans.AddAsync(planB);
        await _context.SaveChangesAsync(default);

        return ((patientA, planA.Id), (patientB, planB.Id));
    }

    private async Task<((Patient Patient, TreatmentPlan Plan) DraftPlan, (Patient Patient, TreatmentPlan Plan) ActivePlan)>
        SeedDraftAndActivePlansAsync()
    {
        var draftContactInfo = ContactInfo.Create("+963911111111", null, true, null).Value;
        var activeContactInfo = ContactInfo.Create("+963922222222", null, true, null).Value;
        var doctorContactInfo = ContactInfo.Create("+963933333333", null, true, null).Value;
        var draftPatient = Patient.Create("Status", "Draft", draftContactInfo, DateTime.UtcNow.AddYears(-42), Domain.Common.Enums.Gender.Male).Value;
        var activePatient = Patient.Create("Status", "Active", activeContactInfo, DateTime.UtcNow.AddYears(-43), Domain.Common.Enums.Gender.Female).Value;
        var doctor = Doctor.Create("Omar", "Ali", "Oral Surgery", doctorContactInfo, Domain.Common.Enums.Gender.Male).Value;
        var draftPlan = TreatmentPlan.Create(draftPatient.Id, doctor.Id, "Status draft plan").Value;
        var activePlan = TreatmentPlan.Create(activePatient.Id, doctor.Id, "Status active plan").Value;
        Assert.True(activePlan.Activate().IsSuccess);

        await _context.Patients.AddAsync(draftPatient);
        await _context.Patients.AddAsync(activePatient);
        await _context.Doctors.AddAsync(doctor);
        await _context.TreatmentPlans.AddAsync(draftPlan);
        await _context.TreatmentPlans.AddAsync(activePlan);
        await _context.SaveChangesAsync(default);

        return ((draftPatient, draftPlan), (activePatient, activePlan));
    }

    private async Task<(Patient Patient, TreatmentPlan Plan, Doctor Doctor)> SeedPlanForScopeTestAsync()
    {
        var contactInfo = ContactInfo.Create("+963911111111", null, true, null).Value;
        var patient = Patient.Create("Scope", "Tester", contactInfo, DateTime.UtcNow.AddYears(-20), Domain.Common.Enums.Gender.Male).Value;
        var doctor = Doctor.Create("Owner", "Doctor", "Prosthodontics", contactInfo, Domain.Common.Enums.Gender.Male).Value;
        var plan = TreatmentPlan.Create(patient.Id, doctor.Id, "Scope plan").Value;

        await _context.Patients.AddAsync(patient);
        await _context.Doctors.AddAsync(doctor);
        await _context.TreatmentPlans.AddAsync(plan);
        await _context.SaveChangesAsync(default);

        return (patient, plan, doctor);
    }
}