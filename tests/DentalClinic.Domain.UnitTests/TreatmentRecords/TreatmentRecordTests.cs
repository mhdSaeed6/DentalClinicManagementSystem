using DentalClinic.Domain.TreatmentRecords;

namespace DentalClinic.Domain.UnitTests.TreatmentRecords;

public class TreatmentRecordTests
{
    [Fact]
    public void Create_ShouldSucceed_WhenToothNumberIsNull()
    {
        // S8: a treatment may be recorded without being tied to a tooth
        // (e.g. a general consultation) — null is a valid state.
        var result = TreatmentRecord.Create(Guid.NewGuid(), Guid.NewGuid(), null, "General consultation", 50);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.ToothNumber);
        Assert.Equal("General consultation", result.Value.ProcedureDetails);
    }

    [Fact]
    public void Create_ShouldSucceed_WhenToothNumberIsValid()
    {
        var result = TreatmentRecord.Create(Guid.NewGuid(), Guid.NewGuid(), 26, "Root Canal Treatment", 260);

        Assert.True(result.IsSuccess);
        Assert.Equal(26, result.Value.ToothNumber);
    }

    [Theory]
    [InlineData(19)] // بين الربعين الأول والثاني
    [InlineData(0)]
    [InlineData(99)] // خارج نطاق الأسنان الدائمة واللبنية
    [InlineData(-11)]
    public void Create_ShouldFail_WhenToothNumberIsProvidedButInvalid(int toothNumber)
    {
        // S8: null is allowed, but a provided value must still pass FDI validation.
        var result = TreatmentRecord.Create(Guid.NewGuid(), Guid.NewGuid(), toothNumber, "Dental Filling", 80);

        Assert.True(result.IsError);
        Assert.Equal(TreatmentRecordErrors.InvalidToothNumber.Code, result.TopError.Code);
    }

    [Fact]
    public void Update_ShouldClearTooth_WhenToothNumberIsNull()
    {
        // Arrange
        var record = TreatmentRecord.Create(Guid.NewGuid(), Guid.NewGuid(), 26, "Composite restoration", 180).Value;

        // Act
        var result = record.Update(record.PatientId, record.DoctorId, null, "Consultation only", 60);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(record.ToothNumber);
    }

    [Theory]
    [InlineData(19)]
    [InlineData(99)]
    public void Update_ShouldFail_WhenToothNumberIsProvidedButInvalid(int toothNumber)
    {
        // Arrange
        var record = TreatmentRecord.Create(Guid.NewGuid(), Guid.NewGuid(), 26, "Composite restoration", 180).Value;

        // Act
        var result = record.Update(record.PatientId, record.DoctorId, toothNumber, "Composite restoration", 180);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(TreatmentRecordErrors.InvalidToothNumber.Code, result.TopError.Code);
        Assert.Equal(26, record.ToothNumber); // القيمة القديمة لا تتغير عند الفشل
    }

    // ------------------------------------------------------------------
    // P1.1 — treatment plans (Option A): plan items are treatment records
    // ------------------------------------------------------------------

    [Fact]
    public void Create_ShouldDefaultToPerformed()
    {
        // D-P3: a directly created record is performed work (ad-hoc).
        var result = TreatmentRecord.Create(Guid.NewGuid(), Guid.NewGuid(), 26, "Composite restoration", 180);

        Assert.True(result.IsSuccess);
        var record = result.Value;
        Assert.Equal(TreatmentRecordStatus.Performed, record.Status);
        Assert.Null(record.PlanId);
        Assert.Null(record.EstimatedCost);
        Assert.Equal(1, record.SessionNumber);
        Assert.True(record.IsBillable); // Performed && Cost > 0
    }

    [Fact]
    public void CreatePlanItem_ShouldBePlannedAndNotBillable()
    {
        var result = TreatmentRecord.CreatePlanItem(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 26, "Root canal", 300, sessionNumber: 2);

        Assert.True(result.IsSuccess);
        var item = result.Value;
        Assert.Equal(TreatmentRecordStatus.Planned, item.Status);
        Assert.NotEqual(Guid.Empty, item.PlanId!.Value);
        Assert.Equal(300, item.EstimatedCost);
        Assert.Equal(0, item.Cost); // لا تكلفة فعلية قبل التنفيذ
        Assert.Equal(2, item.SessionNumber);
        Assert.False(item.IsBillable); // المخطط لا يُفوترة
    }

    [Fact]
    public void CreatePlanItem_ShouldFail_WhenPlanIdIsEmpty()
    {
        var result = TreatmentRecord.CreatePlanItem(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), 26, "Root canal", 300);

        Assert.True(result.IsError);
        Assert.Equal(TreatmentRecordErrors.PlanIdRequired.Code, result.TopError.Code);
    }

    [Theory]
    [InlineData(19)]
    [InlineData(99)]
    public void CreatePlanItem_ShouldValidateToothNumber(int toothNumber)
    {
        var result = TreatmentRecord.CreatePlanItem(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), toothNumber, "Root canal", 300);

        Assert.True(result.IsError);
        Assert.Equal(TreatmentRecordErrors.InvalidToothNumber.Code, result.TopError.Code);
    }

    [Fact]
    public void CreatePlanItem_ShouldFail_WhenEstimateIsNegative()
    {
        var result = TreatmentRecord.CreatePlanItem(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 26, "Root canal", -50);

        Assert.True(result.IsError);
        Assert.Equal(TreatmentRecordErrors.InvalidEstimatedCost.Code, result.TopError.Code);
    }

    [Fact]
    public void CreatePlanItem_ShouldFail_WhenSessionNumberIsInvalid()
    {
        var result = TreatmentRecord.CreatePlanItem(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 26, "Root canal", 300, sessionNumber: 0);

        Assert.True(result.IsError);
        Assert.Equal(TreatmentRecordErrors.InvalidSessionNumber.Code, result.TopError.Code);
    }

    [Fact]
    public void MarkPerformed_ShouldReplaceEstimateWithActualCost()
    {
        // D-P2: estimate is never billed — Cost carries the actual amount.
        var item = TreatmentRecord.CreatePlanItem(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 26, "Root canal", 300, sessionNumber: 1).Value;

        var result = item.MarkPerformed(250);

        Assert.True(result.IsSuccess);
        Assert.Equal(TreatmentRecordStatus.Performed, item.Status);
        Assert.Equal(250, item.Cost);            // التكلفة الفعلية
        Assert.Equal(300, item.EstimatedCost);   // التقدير يبقى كمرجع للطباعة
        Assert.True(item.IsBillable);
    }

    [Fact]
    public void MarkPerformed_ShouldAttachAppointment_WhenProvided()
    {
        var item = TreatmentRecord.CreatePlanItem(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 26, "Root canal", 300).Value;
        var appointmentId = Guid.NewGuid();

        var result = item.MarkPerformed(250, appointmentId);

        Assert.True(result.IsSuccess);
        Assert.Equal(appointmentId, item.AppointmentId);
    }

    [Fact]
    public void MarkPerformed_ShouldFail_WhenActualCostIsNegative()
    {
        var item = TreatmentRecord.CreatePlanItem(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 26, "Root canal", 300).Value;

        var result = item.MarkPerformed(-1);

        Assert.True(result.IsError);
        Assert.Equal(TreatmentRecordErrors.InvalidCost.Code, result.TopError.Code);
        Assert.Equal(TreatmentRecordStatus.Planned, item.Status); // الحالة لا تتغير عند الفشل
    }

    [Fact]
    public void MarkPerformed_ShouldFail_OnPerformedOrCancelledItems()
    {
        var performed = TreatmentRecord.CreatePlanItem(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 26, "Root canal", 300).Value;
        performed.MarkPerformed(250);

        Assert.Equal(TreatmentRecordErrors.ItemAlreadyPerformed.Code, performed.MarkPerformed(300).TopError.Code);

        var cancelled = TreatmentRecord.CreatePlanItem(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 37, "Extraction", 200).Value;
        cancelled.Cancel();

        Assert.Equal(TreatmentRecordErrors.ItemCancelled.Code, cancelled.MarkPerformed(200).TopError.Code);
    }

    [Fact]
    public void Cancel_ShouldMakeItemNotBillable()
    {
        var item = TreatmentRecord.CreatePlanItem(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 26, "Root canal", 300).Value;
        item.MarkPerformed(250);

        Assert.True(item.IsBillable);

        var result = item.Cancel();

        Assert.True(result.IsSuccess);
        Assert.Equal(TreatmentRecordStatus.Cancelled, item.Status);
        Assert.False(item.IsBillable); // الملغي لا يُفوترة أبداً
        Assert.NotEqual(0m, item.Cost); // لكنه لا يزول من السجل
    }

    [Fact]
    public void Cancel_ShouldFail_WhenAlreadyCancelled()
    {
        var item = TreatmentRecord.CreatePlanItem(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 26, "Root canal", 300).Value;
        item.Cancel();

        var result = item.Cancel();

        Assert.True(result.IsError);
        Assert.Equal(TreatmentRecordErrors.ItemAlreadyCancelled.Code, result.TopError.Code);
    }

    [Fact]
    public void Reopen_ShouldMovePerformedBackToPlanned()
    {
        // D-P4: performed → planned is allowed (the issued-invoice guard is
        // enforced in the application layer).
        var item = TreatmentRecord.CreatePlanItem(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 26, "Root canal", 300).Value;
        item.MarkPerformed(250);

        var result = item.Reopen();

        Assert.True(result.IsSuccess);
        Assert.Equal(TreatmentRecordStatus.Planned, item.Status);
        Assert.Equal(300, item.EstimatedCost); // التقدير يبقى متاحاً للتنفيذ مرة ثانية
        Assert.False(item.IsBillable);
    }

    [Theory]
    [InlineData(TreatmentRecordStatus.Planned)]
    [InlineData(TreatmentRecordStatus.Cancelled)]
    public void Reopen_ShouldFail_WhenItemIsNotPerformed(TreatmentRecordStatus status)
    {
        var item = TreatmentRecord.CreatePlanItem(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 26, "Root canal", 300).Value;

        if (status == TreatmentRecordStatus.Cancelled)
        {
            item.Cancel();
        }

        var result = item.Reopen();

        Assert.True(result.IsError);
        Assert.Equal(TreatmentRecordErrors.ItemNotPerformed.Code, result.TopError.Code);
    }

    [Fact]
    public void UpdatePlanItem_ShouldEditEstimateAndSession_WhilePlanned()
    {
        var item = TreatmentRecord.CreatePlanItem(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 26, "Root canal", 300, sessionNumber: 1).Value;

        var result = item.UpdatePlanItem(36, "Root canal + post", 350, sessionNumber: 2);

        Assert.True(result.IsSuccess);
        Assert.Equal(36, item.ToothNumber);
        Assert.Equal(350, item.EstimatedCost);
        Assert.Equal(2, item.SessionNumber);
    }

    [Fact]
    public void UpdatePlanItem_ShouldFail_OncePerformed()
    {
        var item = TreatmentRecord.CreatePlanItem(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 26, "Root canal", 300).Value;
        item.MarkPerformed(250);

        var result = item.UpdatePlanItem(26, "Root canal", 350, sessionNumber: 1);

        Assert.True(result.IsError);
        Assert.Equal(TreatmentRecordErrors.ItemNotPlanned.Code, result.TopError.Code);
        Assert.Equal(250, item.Cost); // التكلفة الفعلية لم تُغيّر
    }
}