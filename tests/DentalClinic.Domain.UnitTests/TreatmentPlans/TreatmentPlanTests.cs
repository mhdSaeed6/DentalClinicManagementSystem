using DentalClinic.Domain.TreatmentPlans;
using DentalClinic.Domain.TreatmentRecords;

namespace DentalClinic.Domain.UnitTests.TreatmentPlans;

public class TreatmentPlanTests
{
    [Fact]
    public void Create_ShouldSucceed_WithValidInput()
    {
        var result = TreatmentPlan.Create(Guid.NewGuid(), Guid.NewGuid(), "Full mouth rehabilitation");

        Assert.True(result.IsSuccess);
        Assert.Equal("Full mouth rehabilitation", result.Value.Title);
        Assert.Equal(TreatmentPlanStatus.Draft, result.Value.Status); // الخطة تبدأ كمسودة
        Assert.Null(result.Value.Notes);
    }

    [Fact]
    public void Create_ShouldTrimTitleAndNotes()
    {
        var result = TreatmentPlan.Create(Guid.NewGuid(), Guid.NewGuid(), "  Crown and bridge  ", "  Follow-up in two sessions  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("Crown and bridge", result.Value.Title);
        Assert.Equal("Follow-up in two sessions", result.Value.Notes);
    }

    [Fact]
    public void Create_ShouldFail_WhenTitleIsBlank()
    {
        var result = TreatmentPlan.Create(Guid.NewGuid(), Guid.NewGuid(), "   ");

        Assert.True(result.IsError);
        Assert.Equal(TreatmentPlanErrors.TitleRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_ShouldFail_WhenIdsAreEmpty()
    {
        Assert.Equal(
            TreatmentPlanErrors.PatientIdRequired.Code,
            TreatmentPlan.Create(Guid.Empty, Guid.NewGuid(), "Plan").TopError.Code);

        Assert.Equal(
            TreatmentPlanErrors.DoctorIdRequired.Code,
            TreatmentPlan.Create(Guid.NewGuid(), Guid.Empty, "Plan").TopError.Code);
    }

    [Fact]
    public void Create_ShouldFail_WhenTitleOrNotesAreTooLong()
    {
        var longTitle = new string('A', 201);
        var longNotes = new string('A', 1001);

        Assert.Equal(
            TreatmentPlanErrors.TitleTooLong.Code,
            TreatmentPlan.Create(Guid.NewGuid(), Guid.NewGuid(), longTitle).TopError.Code);

        Assert.Equal(
            TreatmentPlanErrors.NotesTooLong.Code,
            TreatmentPlan.Create(Guid.NewGuid(), Guid.NewGuid(), "Plan", longNotes).TopError.Code);
    }

    [Fact]
    public void Update_ShouldApplyChanges_WhilePlanIsActive()
    {
        var plan = CreateActivePlan();

        var result = plan.Update("Revised plan", "second session added");

        Assert.True(result.IsSuccess);
        Assert.Equal("Revised plan", plan.Title);
        Assert.Equal("second session added", plan.Notes);
    }

    [Fact]
    public void Update_ShouldFail_AfterPlanIsCompletedOrCancelled()
    {
        var completed = CreateActivePlan();
        completed.Complete();

        Assert.Equal(TreatmentPlanErrors.CannotModifyCompletedPlan.Code, completed.Update("New title").TopError.Code);

        var cancelled = TreatmentPlan.Create(Guid.NewGuid(), Guid.NewGuid(), "Plan").Value;
        cancelled.Cancel();

        Assert.Equal(TreatmentPlanErrors.CannotModifyCancelledPlan.Code, cancelled.Update("New title").TopError.Code);
    }

    [Fact]
    public void Activate_ShouldMoveDraftToActive_Once()
    {
        var plan = TreatmentPlan.Create(Guid.NewGuid(), Guid.NewGuid(), "Plan").Value;

        Assert.True(plan.Activate().IsSuccess);
        Assert.Equal(TreatmentPlanStatus.Active, plan.Status);

        Assert.True(plan.Activate().IsError); // لا تفعيل مزدوج
        Assert.Equal(TreatmentPlanErrors.PlanAlreadyActive.Code, plan.Activate().TopError.Code);
    }

    [Fact]
    public void Complete_ShouldRequireActiveStatus()
    {
        var draft = TreatmentPlan.Create(Guid.NewGuid(), Guid.NewGuid(), "Plan").Value;

        Assert.Equal(TreatmentPlanErrors.PlanNotActive.Code, draft.Complete().TopError.Code); // المسودة لا تُنجز مباشرة

        var cancelled = TreatmentPlan.Create(Guid.NewGuid(), Guid.NewGuid(), "Plan").Value;
        cancelled.Cancel();

        Assert.Equal(TreatmentPlanErrors.CannotModifyCancelledPlan.Code, cancelled.Complete().TopError.Code);
    }

    [Fact]
    public void Complete_ShouldBeTerminal()
    {
        var plan = CreateActivePlan();

        Assert.True(plan.Complete().IsSuccess);
        Assert.Equal(TreatmentPlanStatus.Completed, plan.Status);

        Assert.Equal(TreatmentPlanErrors.PlanAlreadyCompleted.Code, plan.Complete().TopError.Code);
        Assert.Equal(TreatmentPlanErrors.CannotModifyCompletedPlan.Code, plan.Activate().TopError.Code);
    }

    [Fact]
    public void Cancel_ShouldSucceed_WhileDraftOrActive()
    {
        var draft = TreatmentPlan.Create(Guid.NewGuid(), Guid.NewGuid(), "Plan").Value;

        Assert.True(draft.Cancel().IsSuccess);
        Assert.Equal(TreatmentPlanStatus.Cancelled, draft.Status);

        var active = CreateActivePlan();

        Assert.True(active.Cancel().IsSuccess);
        Assert.Equal(TreatmentPlanStatus.Cancelled, active.Status);
    }

    [Fact]
    public void Cancel_ShouldBeTerminal()
    {
        var plan = TreatmentPlan.Create(Guid.NewGuid(), Guid.NewGuid(), "Plan").Value;
        plan.Cancel();

        Assert.Equal(TreatmentPlanErrors.PlanAlreadyCancelled.Code, plan.Cancel().TopError.Code);
        Assert.Equal(TreatmentPlanErrors.CannotModifyCancelledPlan.Code, plan.Activate().TopError.Code);
    }

    [Fact]
    public void CancelledPlan_ShouldNotBeCompletable()
    {
        var plan = TreatmentPlan.Create(Guid.NewGuid(), Guid.NewGuid(), "Plan").Value;
        plan.Activate();
        plan.Cancel();

        Assert.Equal(TreatmentPlanErrors.CannotModifyCancelledPlan.Code, plan.Complete().TopError.Code);
    }

    private static TreatmentPlan CreateActivePlan()
    {
        var plan = TreatmentPlan.Create(Guid.NewGuid(), Guid.NewGuid(), "Active plan").Value;
        plan.Activate();

        return plan;
    }
}