using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Patients;
using DentalClinic.Tests.Common.Patients;
using Xunit;

namespace DentalClinic.Domain.UnitTests.Patients;

public class PatientTests
{
    [Fact]
    public void CreatePatient_ShouldSucceed_WithValidData()
    {
        // Act
        var result = PatientFactory.CreatePatient();

        // Assert
        Assert.True(result.IsSuccess);
        var patient = result.Value;
        Assert.NotNull(patient);
        Assert.Equal("John", patient.FirstName);
        Assert.Equal("Doe", patient.LastName);
        Assert.Equal(Gender.Male, patient.Gender);
        Assert.NotNull(patient.ContactInfo);
        Assert.NotNull(patient.MedicalHistory);
    }

    [Theory]
    [InlineData("invalid-phone")]
    [InlineData("123")]
    [InlineData("+12345678901234567890")] // Too long
    [InlineData(null)]
    public void Create_WithInvalidPhoneNumber_ShouldReturnError(string? invalidPhone)
    {
        // Act
        var result = ContactInfo.Create(invalidPhone!, null, true, null);

        // Assert
        Assert.True(result.IsError);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreatePatient_ShouldFail_WhenFirstNameInvalid(string? invalidFirstName)
    {
        var result = PatientFactory.CreatePatient(firstName: invalidFirstName!);

        Assert.True(result.IsError);
        Assert.Equal(PatientErrors.FirstNameRequired.Code, result.TopError.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreatePatient_ShouldFail_WhenLastNameInvalid(string? invalidLastName)
    {
        var result = PatientFactory.CreatePatient(lastName: invalidLastName!);

        Assert.True(result.IsError);
        Assert.Equal(PatientErrors.LastNameRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void CreatePatient_ShouldFail_WhenContactInfoIsNull()
    {
        // استدعاء الميثود المباشرة للـ Domain لتمرير null وتجاوز القيمة الافتراضية
        var result = Patient.Create("John", "Doe", null!, DateTime.UtcNow.AddYears(-30), Gender.Male);

        Assert.True(result.IsError);
        Assert.Equal(PatientErrors.ContactInfoRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void CreatePatient_ShouldFail_WhenGenderNotDefined()
    {
        Gender invalidGender = (Gender)999;

        var result = PatientFactory.CreatePatient(gender: invalidGender);

        Assert.True(result.IsError);
        Assert.Equal(PatientErrors.GenderRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void CreatePatient_ShouldFail_WhenDateOfBirthIsInFuture()
    {
        var futureDate = DateTime.UtcNow.AddDays(1);

        var result = PatientFactory.CreatePatient(dateOfBirth: futureDate);

        Assert.True(result.IsError);
        Assert.Equal(PatientErrors.InvalidDateOfBirth.Code, result.TopError.Code);
    }

    [Fact]
    public void UpdatePatient_ShouldSuccess_WithValidData()
    {
        var patient = PatientFactory.CreatePatient().Value;

        var newContactInfo = ContactInfo.Create("987654321012", null, true, "updated.com").Value;
        var newDateOfBirth = DateTime.UtcNow.AddYears(-25);

        var result = patient.Update("Jane", "Smith", newContactInfo, newDateOfBirth, Gender.Female);

        Assert.True(result.IsSuccess);
        Assert.Equal("Jane", patient.FirstName);
        Assert.Equal("Smith", patient.LastName);
        Assert.Equal(newContactInfo, patient.ContactInfo);
        Assert.Equal(newDateOfBirth, patient.DateOfBirth);
        Assert.Equal(Gender.Female, patient.Gender);
    }
}