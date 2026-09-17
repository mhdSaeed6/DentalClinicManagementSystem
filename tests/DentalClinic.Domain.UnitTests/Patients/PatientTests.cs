using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Patients;
using Xunit;

namespace DentalClinic.Domain.UnitTests.Patients;

public class PatientTests
{
    [Fact]
    public void CreatePatient_ShouldSucceed_WithValidData()
    {
        var firstName = "John";
        var lastName = "Doe";
        var contactInfo = ContactInfo.Create("741085207410", null, true, "koko.com").Value;
        var dateOfBirth = DateTime.UtcNow.AddYears(-30);

        var result = Patient.Create(firstName, lastName, contactInfo, dateOfBirth, Gender.Male);

        Assert.True(result.IsSuccess);
        var patient = result.Value;
        Assert.NotNull(patient);
        Assert.Equal(firstName, patient.FirstName);
        Assert.Equal(lastName, patient.LastName);
        Assert.Equal(contactInfo, patient.ContactInfo);
        Assert.Equal(dateOfBirth, patient.DateOfBirth);
        Assert.Equal(Gender.Male, patient.Gender);
        Assert.NotNull(patient.MedicalHistory);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreatePatient_ShouldFail_WhenFirstNameInvalid(string? invalidFirstName)
    {
        var contactInfo = ContactInfo.Create("741085207410", null, true, "koko.com").Value;
        var result = Patient.Create(invalidFirstName!, "Doe", contactInfo, DateTime.UtcNow.AddYears(-30), Gender.Male);

        Assert.True(result.IsError);
        Assert.Equal(PatientErrors.FirstNameRequired.Code, result.TopError.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreatePatient_ShouldFail_WhenLastNameInvalid(string? invalidLastName)
    {
        var contactInfo = ContactInfo.Create("741085207410", null, true, "koko.com").Value;
        var result = Patient.Create("John", invalidLastName!, contactInfo, DateTime.UtcNow.AddYears(-30), Gender.Male);

        Assert.True(result.IsError);
        Assert.Equal(PatientErrors.LastNameRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void CreatePatient_ShouldFail_WhenContactInfoIsNull()
    {
        var result = Patient.Create("John", "Doe", null!, DateTime.UtcNow.AddYears(-30), Gender.Male);

        Assert.True(result.IsError);
        Assert.Equal(PatientErrors.ContactInfoRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void CreatePatient_ShouldFail_WhenGenderNotDefined()
    {
        var contactInfo = ContactInfo.Create("741085207410", null, true, "koko.com").Value;
        Gender invalidGender = (Gender)999;

        var result = Patient.Create("John", "Doe", contactInfo, DateTime.UtcNow.AddYears(-30), invalidGender);

        Assert.True(result.IsError);
        Assert.Equal(PatientErrors.GenderRequired.Code, result.TopError.Code);
    }

    [Fact]
    public void CreatePatient_ShouldFail_WhenDateOfBirthIsInFuture()
    {
        var contactInfo = ContactInfo.Create("741085207410", null, true, "koko.com").Value;
        var futureDate = DateTime.UtcNow.AddDays(1);

        var result = Patient.Create("John", "Doe", contactInfo, futureDate, Gender.Male);

        Assert.True(result.IsError);
        Assert.Equal(PatientErrors.InvalidDateOfBirth.Code, result.TopError.Code);
    }

    [Fact]
    public void UpdatePatient_ShouldSuccess_WithValidData()
    {
        var contactInfo = ContactInfo.Create("741085207410", null, true, "koko.com").Value;
        var patient = Patient.Create("John", "Doe", contactInfo, DateTime.UtcNow.AddYears(-30), Gender.Male).Value;

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