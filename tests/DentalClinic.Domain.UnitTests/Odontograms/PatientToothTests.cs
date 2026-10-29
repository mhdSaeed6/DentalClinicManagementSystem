using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Odontograms;

namespace DentalClinic.Domain.UnitTests.Odontograms;

public class PatientToothTests
{
    [Fact]
    public void Create_ShouldSucceed_WhenPermanentToothIsValid()
    {
        // Arrange
        var patientId = Guid.NewGuid();

        // Act
        var result = PatientTooth.Create(patientId, 26, ToothCondition.Caries, ToothSurface.Occlusal, "تسوّس سطحي");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(patientId, result.Value.PatientId);
        Assert.Equal(26, result.Value.ToothNumber);
        Assert.Equal(ToothCondition.Caries, result.Value.Condition);
        Assert.Equal(ToothSurface.Occlusal, result.Value.Surface);
        Assert.Equal("تسوّس سطحي", result.Value.Notes);
        Assert.False(result.Value.IsDeleted);
    }

    [Fact]
    public void Create_ShouldSucceed_WhenPediatricToothIsValid()
    {
        // Act
        var result = PatientTooth.Create(Guid.NewGuid(), 51, ToothCondition.Healthy);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(51, result.Value.ToothNumber);
        Assert.Equal(ToothCondition.Healthy, result.Value.Condition);
        Assert.Null(result.Value.Surface);
        Assert.Null(result.Value.Notes);
    }

    [Theory]
    [InlineData(10)] // خارج حدود كل الأرباع
    [InlineData(19)] // بين الربعين الأول والثاني
    [InlineData(50)] // قبل الأرباع اللبنية
    [InlineData(86)] // بعد الأرباع اللبنية
    [InlineData(0)]
    public void Create_ShouldFail_WhenToothNumberIsInvalid(int toothNumber)
    {
        // Act
        var result = PatientTooth.Create(Guid.NewGuid(), toothNumber, ToothCondition.Caries);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(PatientToothErrors.InvalidToothNumber.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_ShouldFail_WhenPatientIdIsEmpty()
    {
        // Act
        var result = PatientTooth.Create(Guid.Empty, 26, ToothCondition.Caries);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(PatientToothErrors.PatientIdRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_ShouldFail_WhenConditionIsUndefined()
    {
        // Act
        var result = PatientTooth.Create(Guid.NewGuid(), 26, (ToothCondition)99);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(PatientToothErrors.InvalidCondition.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_ShouldFail_WhenSurfaceIsUndefined()
    {
        // Act
        var result = PatientTooth.Create(Guid.NewGuid(), 26, ToothCondition.Caries, (ToothSurface)99);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(PatientToothErrors.InvalidSurface.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_ShouldFail_WhenNotesExceed500Characters()
    {
        // Arrange
        var notes = new string('x', 501);

        // Act
        var result = PatientTooth.Create(Guid.NewGuid(), 26, ToothCondition.Caries, null, notes);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(PatientToothErrors.NotesTooLong.Code, result.TopError.Code);
    }

    [Fact]
    public void Update_ShouldReplaceState_WhenNewStateIsValid()
    {
        // Arrange
        var tooth = PatientTooth.Create(Guid.NewGuid(), 26, ToothCondition.Caries, ToothSurface.Occlusal).Value;

        // Act
        var result = tooth.Update(ToothCondition.Filled, ToothSurface.Occlusal, "حشوة دائمة");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ToothCondition.Filled, tooth.Condition);
        Assert.Equal(ToothSurface.Occlusal, tooth.Surface);
        Assert.Equal("حشوة دائمة", tooth.Notes);
    }

    [Fact]
    public void Update_ShouldFail_WhenConditionIsUndefined()
    {
        // Arrange
        var tooth = PatientTooth.Create(Guid.NewGuid(), 26, ToothCondition.Caries).Value;

        // Act
        var result = tooth.Update((ToothCondition)99);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(PatientToothErrors.InvalidCondition.Code, result.TopError.Code);
    }

    [Fact]
    public void Delete_ShouldSetSoftDeleteFields()
    {
        // Arrange
        var tooth = PatientTooth.Create(Guid.NewGuid(), 26, ToothCondition.Caries).Value;

        // Act
        tooth.Delete("admin-user");

        // Assert
        Assert.True(tooth.IsDeleted);
        Assert.NotNull(tooth.DeletedAtUtc);
        Assert.Equal("admin-user", tooth.DeletedBy);
    }
}