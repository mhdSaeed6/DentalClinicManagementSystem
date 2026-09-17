using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Doctors;
using DentalClinic.Tests.Common.Doctors;
using Xunit;

namespace DentalClinic.Domain.UnitTests.Doctors;

public class DoctorTests
{
    [Fact]
    public void CreateDoctor_ShouldSuccess_WithValidData()
    {
        // Arrange
        var contactInfo = ContactInfo.Create("741085207410", null, true, "koko.com").Value;

        // Act
        var result = DoctorFactory.CreateDoctor(
            firstName: "John",
            lastName: "Doe",
            specialization: "Cardiology",
            contactInfo: contactInfo,
            gender: Gender.Male);

        // Assert
        Assert.True(result.IsSuccess);
        var doctor = result.Value;
        Assert.NotNull(doctor);
        Assert.Equal("John", doctor.FirstName);
        Assert.Equal("Doe", doctor.LastName);
        Assert.Equal("Cardiology", doctor.Specialization);
        Assert.Equal(Gender.Male, doctor.Gender);
        Assert.True(doctor.IsActive);
        Assert.NotNull(doctor.ContactInfo);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateDoctor_ShouldFail_WhenFirstNameInvalid(string? invalidFirstName)
    {
        var result = DoctorFactory.CreateDoctor(firstName: invalidFirstName!);

        Assert.True(result.IsError);
        Assert.Equal(DoctorErrors.FirstNameRequired.Code, result.TopError.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateDoctor_ShouldFail_WhenLastNameInvalid(string? invalidLastName)
    {
        var result = DoctorFactory.CreateDoctor(lastName: invalidLastName!);

        Assert.True(result.IsError);
        Assert.Equal(DoctorErrors.LastNameRequired.Code, result.TopError.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateDoctor_ShouldFail_WhenSpecializationInvalid(string? invalidSpecialization)
    {
        var result = DoctorFactory.CreateDoctor(specialization: invalidSpecialization!);

        Assert.True(result.IsError);
        Assert.Equal(DoctorErrors.SpecializationRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void CreateDoctor_ShouldFail_WhenContactInfoIsNull()
    {
        // تمرير null مباشرة لتخطي القيمة الافتراضية للـ Factory واختبار الـ Domain
        var result = Doctor.Create("John", "Doe", "Cardiology", null!, Gender.Male);

        Assert.True(result.IsError);
        Assert.Equal(DoctorErrors.ContactInfoRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void CreateDoctor_ShouldFail_WhenGenderNotDefined()
    {
        // لاختبار الـ Enum غير المعرّف (Enum.IsDefined)، نقوم بعمل Cast لقيمة رقمية غير موجودة في الـ Enum
        Gender invalidGender = (Gender)999;
        var contactInfo = ContactInfo.Create("741085207410", null, true, "koko.com").Value;

        var result = Doctor.Create("John", "Doe", "Cardiology", contactInfo, invalidGender);

        Assert.True(result.IsError);
        Assert.Equal(DoctorErrors.GenderRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void UpdateDoctor_ShouldSuccess_WithValidData()
    {
        // Arrange
        var doctor = DoctorFactory.CreateDoctor().Value;
        var newContactInfo = ContactInfo.Create("987654321012", null, true, "updated.com").Value;

        // Act
        var result = doctor.Update("Jane", "Smith", "Pediatrics", newContactInfo, Gender.Female);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Jane", doctor.FirstName);
        Assert.Equal("Smith", doctor.LastName);
        Assert.Equal("Pediatrics", doctor.Specialization);
        Assert.Equal(Gender.Female, doctor.Gender);
        Assert.Equal(newContactInfo, doctor.ContactInfo);
    }

    [Fact]
    public void DeactivateAndActivate_ShouldChangeIsActiveState()
    {
        var doctor = DoctorFactory.CreateDoctor().Value;
        Assert.True(doctor.IsActive);

        doctor.Deactivate();
        Assert.False(doctor.IsActive);

        doctor.Activate();
        Assert.True(doctor.IsActive);
    }
}