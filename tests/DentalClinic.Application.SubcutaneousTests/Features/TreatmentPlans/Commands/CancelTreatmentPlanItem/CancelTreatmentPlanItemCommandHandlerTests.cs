using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.TreatmentPlans.Commands.CancelTreatmentPlanItem;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Doctors;
using DentalClinic.Domain.Patients;
using DentalClinic.Domain.TreatmentPlans;
using DentalClinic.Domain.TreatmentRecords;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.TreatmentPlans.Commands.CancelTreatmentPlanItem;

[Collection(WebAppFactoryCollection.CollectionName)]
public class CancelTreatmentPlanItemCommandHandlerTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    [Fact]
    public async Task Handle_PerformedItem_ShouldCancelAndStopBilling()
    {
        var (_, item) = await SeedPerformerPlanAsync(perform: true);

        var result = await _mediator.Send(new CancelTreatmentPlanItemCommand(item.PlanId!.Value, item.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal(TreatmentRecordStatus.Cancelled, result.Value.Status);

        var dbItem = await _context.TreatmentRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == item.Id);

        Assert.NotNull(dbItem);
        Assert.False(dbItem.IsBillable);
        Assert.Equal(250m, dbItem.Cost); // الملغي يبقى في السجل ولا يزول من التاريخ
    }

    [Fact]
    public async Task Handle_Twice_ShouldReturnConflict()
    {
        var (_, item) = await SeedPerformerPlanAsync(perform: false);

        var first = await _mediator.Send(new CancelTreatmentPlanItemCommand(item.PlanId!.Value, item.Id));
        Assert.True(first.IsSuccess);

        var second = await _mediator.Send(new CancelTreatmentPlanItemCommand(item.PlanId!.Value, item.Id));

        Assert.True(second.IsError);
        Assert.Equal(TreatmentRecordErrors.ItemAlreadyCancelled.Code, second.TopError.Code);
    }

    private async Task<(TreatmentPlan Plan, TreatmentRecord Item)> SeedPerformerPlanAsync(bool perform)
    {
        var contactInfo = ContactInfo.Create("+963911111111", null, true, null).Value;
        var patient = Patient.Create("Ahmad", "Kareem", contactInfo, DateTime.UtcNow.AddYears(-25), Domain.Common.Enums.Gender.Male).Value;
        var doctor = Doctor.Create("Omar", "Ali", "Endodontics", contactInfo, Domain.Common.Enums.Gender.Male).Value;
        var plan = TreatmentPlan.Create(patient.Id, doctor.Id, "Plan with cancel flow").Value;
        var item = TreatmentRecord.CreatePlanItem(plan.Id, patient.Id, doctor.Id, 45, "Bridge", 200m).Value;

        if (perform)
        {
            Assert.True(item.MarkPerformed(250m).IsSuccess);
        }

        await _context.Patients.AddAsync(patient);
        await _context.Doctors.AddAsync(doctor);
        await _context.TreatmentPlans.AddAsync(plan);
        await _context.TreatmentRecords.AddAsync(item);
        await _context.SaveChangesAsync(default);

        return (plan, item);
    }
}