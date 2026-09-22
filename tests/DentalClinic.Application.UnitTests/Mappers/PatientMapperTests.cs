using DentalClinic.Application.Features.Patients.Dtos;
using DentalClinic.Application.Features.Patients.Mappers;
using DentalClinic.Domain.Patients;
using DentalClinic.Tests.Common.Patients;

using Xunit;

namespace DentalClinic.Application.UnitTests.Mappers;

public class PatientMapperTests
{
    [Fact]
    public void ToDto_ShouldMapCorrectly()
    {
        // Arrange
        var patient = PatientFactory.CreatePatient().Value;

        // Act
        var dto = patient.ToDto();

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(patient.Id, dto.Id);
        Assert.Equal(patient.FirstName, dto.FirstName);
        Assert.Equal(patient.LastName, dto.LastName);
        Assert.Equal(patient.ContactInfo.PrimaryPhone, dto.PrimaryPhone);
        Assert.Equal(patient.ContactInfo.SecondaryPhone, dto.SecondaryPhone);
        Assert.Equal(patient.ContactInfo.HasWhatsAppOnPrimary, dto.HasWhatsAppOnPrimary);
        Assert.Equal(patient.DateOfBirth, dto.DateOfBirth);
        Assert.Equal((int)patient.Gender, dto.Gender);

        Assert.NotNull(dto.MedicalHistory);
        Assert.Equal(patient.MedicalHistory.HasDiabetes, dto.MedicalHistory.HasDiabetes);
        Assert.Equal(patient.MedicalHistory.HasHypertension, dto.MedicalHistory.HasHypertension);
        Assert.Equal(patient.MedicalHistory.HasHeartDisease, dto.MedicalHistory.HasHeartDisease);
        Assert.Equal(patient.MedicalHistory.Allergies, dto.MedicalHistory.Allergies);
        Assert.Equal(patient.MedicalHistory.Notes, dto.MedicalHistory.Notes);
    }

    [Fact]
    public void ToDto_WhenPatientIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        Patient patient = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => patient.ToDto());
    }

    [Fact]
    public void MedicalHistoryToDto_WhenMedicalHistoryIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        MedicalHistory medicalHistory = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => medicalHistory.ToDto());
    }

    [Fact]
    public void ToDtos_ShouldMapListCorrectly()
    {
        // Arrange
        var patient1 = PatientFactory.CreatePatient().Value;
        var patient2 = PatientFactory.CreatePatient().Value;
        var patients = new List<Patient> { patient1, patient2 };

        // Act
        var dtos = patients.ToDtos();

        // Assert
        Assert.NotNull(dtos);
        Assert.Equal(2, dtos.Count);
        Assert.Equal(patient1.Id, dtos[0].Id);
        Assert.Equal(patient2.Id, dtos[1].Id);
    }

    [Fact]
    public void ToDtos_WhenListIsEmpty_ShouldReturnEmptyList()
    {
        // Arrange
        var patients = new List<Patient>();

        // Act
        var dtos = patients.ToDtos();

        // Assert
        Assert.NotNull(dtos);
        Assert.Empty(dtos);
    }
}