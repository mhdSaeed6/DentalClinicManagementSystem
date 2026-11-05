using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.TreatmentPlans.Commands.AddTreatmentPlanItem;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Doctors;
using DentalClinic.Domain.Patients;
using DentalClinic.Domain.TreatmentPlans;
using DentalClinic.Domain.TreatmentRecords;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.TreatmentPlans.Commands.AddTreatmentPlanItem;

[Collection(WebAppFactoryCollection.CollectionName)]
public class AddTreatmentPlanItemCommandHandlerTests(WebAppFactory factory) : IDisposable
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    // The shared TestCurrentUser is mutated by the role-switching tests and
    // must be reset to Admin so the following tests keep their default caller.
    public void Dispose() => factory.CurrentUser.AsAdmin();

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreatePlannedItem()
    {
        // Arrange
        var (plan, patientId) = await SeedPlanAndCleanupAsync();

        var command = new AddTreatmentPlanItemCommand(plan.Id, 26, "Root canal treatment", 300m, SessionNumber: 2);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsSuccess);
        var item = result.Value;
        Assert.Equal(plan.Id, item.PlanId);
        Assert.Equal(TreatmentRecordStatus.Planned, item.Status);
        Assert.Equal(300m, item.EstimatedCost);
        Assert.Equal(0m, item.Cost);
        Assert.Equal(2, item.SessionNumber);
        Assert.Equal(patientId, item.PatientId); // يتوافق دائماً مع المريض — القيمة ليست من الطلب

        var dbItem = await _context.TreatmentRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == item.Id);

        Assert.NotNull(dbItem);
        Assert.Equal(TreatmentRecordStatus.Planned, dbItem.Status);
        Assert.False(dbItem.IsBillable);
    }

    [Fact]
    public async Task Handle_WithNonExistingPlan_ShouldReturnNotFound()
    {
        var result = await _mediator.Send(new AddTreatmentPlanItemCommand(Guid.NewGuid(), 26, "Root canal", 300));

        Assert.True(result.IsError);
        Assert.Equal("TreatmentPlan.NotFound", result.TopError.Code);
    }

    // DoctorId derives from the plan — a doctor cannot inject items into
    // another doctor's plan even with the connection policy allowing writes.
    [Fact]
    public async Task Handle_DoctorTouchingAnotherDoctorsPlan_ShouldBeForbidden()
    {
        var (plan, _) = await SeedPlanAndCleanupAsync();
        factory.CurrentUser.AsDoctor(Guid.NewGuid());

        var result = await _mediator.Send(new AddTreatmentPlanItemCommand(plan.Id, 26, "Root canal", 300));

        Assert.True(result.IsError);
        Assert.Equal("TreatmentPlan.AccessForbidden", result.TopError.Code);
    }

    private async Task<(TreatmentPlan Plan, Guid PatientId)> SeedPlanAndCleanupAsync()
    {
        var contactInfo = ContactInfo.Create("+963911111111", null, true, null).Value;
        var patient = Patient.Create("Ahmad", "Kareem", contactInfo, DateTime.UtcNow.AddYears(-25), Domain.Common.Enums.Gender.Male).Value;
        var doctor = Doctor.Create("Omar", "Ali", "Endodontics", contactInfo, Domain.Common.Enums.Gender.Male).Value;
        var plan = TreatmentPlan.Create(patient.Id, doctor.Id, "Rehabilitation plan").Value;

        await _context.Patients.AddAsync(patient);
        await _context.Doctors.AddAsync(doctor);
        await _context.TreatmentPlans.AddAsync(plan);
        await _context.SaveChangesAsync(default);

        return (plan, patient.Id);
    }
}