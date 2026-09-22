using DentalClinic.Application.Features.Doctors.Dtos;
using DentalClinic.Application.Features.Doctors.Mappers;
using DentalClinic.Domain.Doctors;
using DentalClinic.Tests.Common.Doctors;

using Xunit;

namespace DentalClinic.Application.UnitTests.Mappers;

public class DoctorMapperTests
{
    [Fact]
    public void ToDto_ShouldMapCorrectly()
    {
        // Arrange
        var doctor = DoctorFactory.CreateDoctor().Value;

        // Act
        var dto = doctor.ToDto();

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(doctor.Id, dto.Id);
        Assert.Equal(doctor.FirstName, dto.FirstName);
        Assert.Equal(doctor.LastName, dto.LastName);
        Assert.Equal(doctor.Specialization, dto.Specialization);
        Assert.Equal(doctor.ContactInfo.PrimaryPhone, dto.PrimaryPhone);
        Assert.Equal(doctor.ContactInfo.SecondaryPhone, dto.SecondaryPhone);
        Assert.Equal(doctor.ContactInfo.HasWhatsAppOnPrimary, dto.HasWhatsAppOnPrimary);
        Assert.Equal(doctor.ContactInfo.SocialMediaLink, dto.SocialMediaLink);
        Assert.Equal(doctor.Gender, dto.Gender);
        Assert.Equal(doctor.IsActive, dto.IsActive);
    }

    [Fact]
    public void ToDto_WhenEntityIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        Doctor doctor = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => doctor.ToDto());
    }

    [Fact]
    public void ToDtos_ShouldMapListCorrectly()
    {
        // Arrange
        var doctor1 = DoctorFactory.CreateDoctor().Value;
        var doctor2 = DoctorFactory.CreateDoctor().Value;
        var doctors = new List<Doctor> { doctor1, doctor2 };

        // Act
        var dtos = doctors.ToDtos();

        // Assert
        Assert.NotNull(dtos);
        Assert.Equal(2, dtos.Count);
        Assert.Equal(doctor1.Id, dtos[0].Id);
        Assert.Equal(doctor2.Id, dtos[1].Id);
    }

    [Fact]
    public void ToDtos_WhenListIsEmpty_ShouldReturnEmptyList()
    {
        // Arrange
        var doctors = new List<Doctor>();

        // Act
        var dtos = doctors.ToDtos();

        // Assert
        Assert.NotNull(dtos);
        Assert.Empty(dtos);
    }
}