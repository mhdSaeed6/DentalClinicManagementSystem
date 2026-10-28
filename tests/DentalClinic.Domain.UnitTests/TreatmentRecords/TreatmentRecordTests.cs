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
}