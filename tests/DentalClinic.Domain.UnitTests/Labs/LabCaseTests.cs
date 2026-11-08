using DentalClinic.Domain.Labs;

namespace DentalClinic.Domain.UnitTests.Labs;

public class LabCaseTests
{
    private static readonly DateTimeOffset SentAt = new(2026, 11, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ExpectedAt = SentAt.AddDays(10);

    [Fact]
    public void Create_ShouldSucceed_WithValidInput()
    {
        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();

        var result = LabCase.Create(patientId, doctorId, "  Smile Lab  ", "  Crown  ", SentAt, ExpectedAt);

        Assert.True(result.IsSuccess);
        Assert.Equal(patientId, result.Value.PatientId);
        Assert.Equal(doctorId, result.Value.DoctorId);
        Assert.Equal("Smile Lab", result.Value.LabName);
        Assert.Equal("Crown", result.Value.CaseType);
        Assert.Equal(LabCaseStatus.Sent, result.Value.Status);
        Assert.Equal(SentAt, result.Value.SentAtUtc);
        Assert.Equal(ExpectedAt, result.Value.ExpectedReturnAtUtc);
        Assert.Null(result.Value.ReturnedAtUtc);
        Assert.Null(result.Value.Notes);
        Assert.Equal(0m, result.Value.Cost);
    }

    [Fact]
    public void Create_ShouldTrimNotesAndKeepOptionalFields()
    {
        var treatmentRecordId = Guid.NewGuid();

        var result = LabCase.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Lab",
            "Bridge",
            SentAt,
            ExpectedAt,
            treatmentRecordId,
            16,
            450.50m,
            "  shade A2  ");

        Assert.True(result.IsSuccess);
        Assert.Equal(treatmentRecordId, result.Value.TreatmentRecordId);
        Assert.Equal(16, result.Value.ToothNumber);
        Assert.Equal(450.50m, result.Value.Cost);
        Assert.Equal("shade A2", result.Value.Notes);
    }

    [Fact]
    public void Create_ShouldFail_WhenPatientOrDoctorIsEmpty()
    {
        Assert.Equal(
            LabCaseErrors.PatientIdRequired.Code,
            LabCase.Create(Guid.Empty, Guid.NewGuid(), "Lab", "Crown", SentAt, ExpectedAt).TopError.Code);

        Assert.Equal(
            LabCaseErrors.DoctorIdRequired.Code,
            LabCase.Create(Guid.NewGuid(), Guid.Empty, "Lab", "Crown", SentAt, ExpectedAt).TopError.Code);
    }

    [Fact]
    public void Create_ShouldFail_WhenLabOrCaseTypeIsBlank()
    {
        Assert.Equal(
            LabCaseErrors.LabNameRequired.Code,
            LabCase.Create(Guid.NewGuid(), Guid.NewGuid(), "  ", "Crown", SentAt, ExpectedAt).TopError.Code);

        Assert.Equal(
            LabCaseErrors.CaseTypeRequired.Code,
            LabCase.Create(Guid.NewGuid(), Guid.NewGuid(), "Lab", "  ", SentAt, ExpectedAt).TopError.Code);
    }

    [Fact]
    public void Create_ShouldFail_WithInvalidToothCostOrDates()
    {
        Assert.Equal(
            LabCaseErrors.InvalidToothNumber.Code,
            LabCase.Create(Guid.NewGuid(), Guid.NewGuid(), "Lab", "Crown", SentAt, ExpectedAt, toothNumber: 99).TopError.Code);

        Assert.Equal(
            LabCaseErrors.InvalidCost.Code,
            LabCase.Create(Guid.NewGuid(), Guid.NewGuid(), "Lab", "Crown", SentAt, ExpectedAt, cost: -1m).TopError.Code);

        Assert.Equal(
            LabCaseErrors.InvalidPlannedDates.Code,
            LabCase.Create(Guid.NewGuid(), Guid.NewGuid(), "Lab", "Crown", SentAt, SentAt.AddDays(-1)).TopError.Code);
    }

    [Fact]
    public void Update_ShouldApplyChanges_WhileCaseIsOpen()
    {
        var labCase = CreateOpenCase();

        var result = labCase.Update(
            "New Lab",
            "Implant",
            ExpectedAt.AddDays(2),
            11,
            300m,
            "new notes",
            null);

        Assert.True(result.IsSuccess);
        Assert.Equal("New Lab", labCase.LabName);
        Assert.Equal("Implant", labCase.CaseType);
        Assert.Equal(ExpectedAt.AddDays(2), labCase.ExpectedReturnAtUtc);
        Assert.Equal(11, labCase.ToothNumber);
        Assert.Equal(300m, labCase.Cost);
        Assert.Equal("new notes", labCase.Notes);
    }

    [Fact]
    public void Update_ShouldFail_WhenCaseIsReturnedOrFailed()
    {
        var returned = CreateReturnedCase();
        var failed = CreateFailedCase();

        Assert.Equal(
            LabCaseErrors.CaseClosed.Code,
            returned.Update("Lab", "Crown", ExpectedAt, null, 0, null, null).TopError.Code);

        Assert.Equal(
            LabCaseErrors.CaseClosed.Code,
            failed.Update("Lab", "Crown", ExpectedAt, null, 0, null, null).TopError.Code);
    }

    [Fact]
    public void ChangeStatus_ShouldWalkTheHappyPath_AndStampTheReturnDate()
    {
        var labCase = CreateOpenCase();
        var returnedAt = SentAt.AddDays(9);

        Assert.True(labCase.ChangeStatus(LabCaseStatus.AtLab, SentAt.AddDays(1)).IsSuccess);
        Assert.True(labCase.ChangeStatus(LabCaseStatus.Trial, SentAt.AddDays(5)).IsSuccess);
        Assert.True(labCase.ChangeStatus(LabCaseStatus.TriedIn, SentAt.AddDays(6)).IsSuccess);
        Assert.True(labCase.ChangeStatus(LabCaseStatus.Returned, returnedAt).IsSuccess);

        Assert.Equal(LabCaseStatus.Returned, labCase.Status);
        Assert.Equal(returnedAt, labCase.ReturnedAtUtc);
    }

    [Fact]
    public void ChangeStatus_ShouldRejectTheSameStatus_AndIllegalTransitions()
    {
        var labCase = CreateOpenCase();

        Assert.Equal(
            LabCaseErrors.StatusAlreadySet.Code,
            labCase.ChangeStatus(LabCaseStatus.Sent, SentAt.AddHours(1)).TopError.Code);

        // Sent cannot jump straight to Returned; it must pass through the lab.
        Assert.Equal(
            LabCaseErrors.InvalidStatusTransition.Code,
            labCase.ChangeStatus(LabCaseStatus.Returned, SentAt.AddDays(9)).TopError.Code);
    }

    [Fact]
    public void ChangeStatus_ShouldRejectReturnBeforeTheSendDate()
    {
        var labCase = CreateOpenCase();
        labCase.ChangeStatus(LabCaseStatus.AtLab, SentAt.AddDays(1));

        Assert.Equal(
            LabCaseErrors.InvalidReturnedAt.Code,
            labCase.ChangeStatus(LabCaseStatus.Returned, SentAt.AddHours(-1)).TopError.Code);
    }

    [Fact]
    public void ChangeStatus_ShouldRejectAnyChange_AfterTheCaseIsReturned()
    {
        var labCase = CreateReturnedCase();

        Assert.Equal(
            LabCaseErrors.InvalidStatusTransition.Code,
            labCase.ChangeStatus(LabCaseStatus.AtLab, ExpectedAt).TopError.Code);
    }

    [Fact]
    public void ChangeStatus_FromFailedToSent_ShouldRestartTheLifecycle()
    {
        var labCase = CreateFailedCase();
        var remakeSentAt = SentAt.AddDays(14);

        var result = labCase.ChangeStatus(LabCaseStatus.Sent, remakeSentAt);

        Assert.True(result.IsSuccess);
        Assert.Equal(LabCaseStatus.Sent, labCase.Status);
        Assert.Equal(remakeSentAt, labCase.SentAtUtc);
        Assert.Null(labCase.ReturnedAtUtc);
    }

    [Fact]
    public void IsOverdue_ShouldBeTrue_OnlyForOpenCasesPastTheExpectedDate()
    {
        var open = CreateOpenCase();

        Assert.False(open.IsOverdue(ExpectedAt.AddDays(-1)));
        Assert.False(open.IsOverdue(ExpectedAt));

        var pastDue = ExpectedAt.AddDays(1);
        Assert.True(open.IsOverdue(pastDue));

        var returned = CreateReturnedCase();
        Assert.False(returned.IsOverdue(pastDue));

        var failed = CreateFailedCase();
        Assert.False(failed.IsOverdue(pastDue));
    }

    private static LabCase CreateOpenCase()
    {
        var result = LabCase.Create(Guid.NewGuid(), Guid.NewGuid(), "Lab", "Crown", SentAt, ExpectedAt);
        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private static LabCase CreateReturnedCase()
    {
        var labCase = CreateOpenCase();
        labCase.ChangeStatus(LabCaseStatus.AtLab, SentAt.AddDays(1));
        labCase.ChangeStatus(LabCaseStatus.Returned, SentAt.AddDays(5));
        return labCase;
    }

    private static LabCase CreateFailedCase()
    {
        var labCase = CreateOpenCase();
        labCase.ChangeStatus(LabCaseStatus.Failed, SentAt.AddDays(2));
        return labCase;
    }
}