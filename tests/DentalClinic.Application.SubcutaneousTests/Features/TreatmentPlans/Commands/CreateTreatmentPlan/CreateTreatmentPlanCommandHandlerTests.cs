using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.TreatmentPlans.Commands.CreateTreatmentPlan;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Doctors;
using DentalClinic.Domain.Patients;
using DentalClinic.Domain.TreatmentPlans;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.TreatmentPlans.Commands.CreateTreatmentPlan;

[Collection(WebAppFactoryCollection.CollectionName)]
public class CreateTreatmentPlanCommandHandlerTests(WebAppFactory factory) : IDisposable
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    // The shared TestCurrentUser is mutated by the role-switching tests and
    // must be reset to Admin so the following tests keep their default caller.
    public void Dispose() => factory.CurrentUser.AsAdmin();

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateDraftPlan()
    {
        // Arrange
        var (patient, doctor) = await SeedPatientAndDoctorAsync();
        var command = new CreateTreatmentPlanCommand(patient.Id, doctor.Id, "Full mouth rehabilitation", "Second session after healing.");

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsSuccess);
        var plan = result.Value;
        Assert.Equal("Full mouth rehabilitation", plan.Title);
        Assert.Equal(TreatmentPlanStatus.Draft, plan.Status);
        Assert.Equal(patient.Id, plan.PatientId);
        Assert.Equal(doctor.Id, plan.DoctorId);

        var dbPlan = await _context.TreatmentPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == plan.Id);

        Assert.NotNull(dbPlan);
        Assert.Equal(TreatmentPlanStatus.Draft, dbPlan.Status);
        Assert.Equal("Second session after healing.", dbPlan.Notes);
    }

    [Fact]
    public async Task Handle_WithNonExistingPatient_ShouldReturnError()
    {
        var (_, doctor) = await SeedPatientAndDoctorAsync();
        var command = new CreateTreatmentPlanCommand(Guid.NewGuid(), doctor.Id, "Plan for a ghost patient", null);

        var result = await _mediator.Send(command);

        Assert.True(result.IsError);
    }

    [Fact]
    public async Task Handle_WithNonExistingDoctor_ShouldReturnError()
    {
        var (patient, _) = await SeedPatientAndDoctorAsync();
        var command = new CreateTreatmentPlanCommand(patient.Id, Guid.NewGuid(), "Plan by a ghost doctor", null);

        var result = await _mediator.Send(command);

        Assert.True(result.IsError);
    }

    // A doctor may only author plans for their own profile (two-layer rule);
    // the shared TestCurrentUser is reset to Admin by other tests.
    [Fact]
    public async Task Handle_DoctorAuthoringForAnotherDoctor_ShouldBeForbidden()
    {
        var (patient, otherDoctor) = await SeedPatientAndDoctorAsync();
        factory.CurrentUser.AsDoctor(Guid.NewGuid());

        var command = new CreateTreatmentPlanCommand(patient.Id, otherDoctor.Id, "Unauthorized plan", null);
        var result = await _mediator.Send(command);

        Assert.True(result.IsError);
        Assert.Equal("TreatmentPlan.AccessForbidden", result.TopError.Code);
    }

    private async Task<(Patient Patient, Doctor Doctor)> SeedPatientAndDoctorAsync()
    {
        var contactInfo = ContactInfo.Create("+963911111111", null, true, null).Value;
        var patient = Patient.Create("Ahmad", "Kareem", contactInfo, DateTime.UtcNow.AddYears(-25), Domain.Common.Enums.Gender.Male).Value;
        var doctor = Doctor.Create("Omar", "Ali", "Endodontics", contactInfo, Domain.Common.Enums.Gender.Male).Value;

        await _context.Patients.AddAsync(patient);
        await _context.Doctors.AddAsync(doctor);
        await _context.SaveChangesAsync(default);

        return (patient, doctor);
    }
}