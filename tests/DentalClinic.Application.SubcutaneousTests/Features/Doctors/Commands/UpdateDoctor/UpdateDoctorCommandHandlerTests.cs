using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Doctors.Commands.UpdateDoctor;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Doctors;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Doctors.Commands.UpdateDoctor;

[Collection(WebAppFactoryCollection.CollectionName)]
public class UpdateDoctorCommandHandlerTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    [Fact]
    public async Task Handle_WithValidData_ShouldSucceedAndUpdateDoctorInDatabase()
    {
        // Arrange
        var initialContact = ContactInfo.Create("+963911111111", null, true, null).Value;
        var doctor = Doctor.Create("InitialFirst", "InitialLast", "General", initialContact, Gender.Male).Value;

        await _context.Doctors.AddAsync(doctor);
        await _context.SaveChangesAsync(default);

        var updatedContact = ContactInfo.Create("+963922222222", "+963933333333", true, "https://facebook.com/updated").Value;
        var command = new UpdateDoctorCommand(
            DoctorId: doctor.Id,
            FirstName: "UpdatedFirst",
            LastName: "UpdatedLast",
            Specialization: "Orthodontics",
            ContactInfo: updatedContact,
            Gender: Gender.Male);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("UpdatedFirst", result.Value.FirstName);
        Assert.Equal("UpdatedLast", result.Value.LastName);
        Assert.Equal("Orthodontics", result.Value.Specialization);

        // Subcutaneous DB verification
        var doctorInDb = await _context.Doctors
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == doctor.Id);

        Assert.NotNull(doctorInDb);
        Assert.Equal("UpdatedFirst", doctorInDb.FirstName);
        Assert.Equal("UpdatedLast", doctorInDb.LastName);
        Assert.Equal("Orthodontics", doctorInDb.Specialization);
    }

    [Fact]
    public async Task Handle_WithNonExistingDoctorId_ShouldFailWithDoctorNotFound()
    {
        // Arrange
        var contact = ContactInfo.Create("+963911111111", null, true, null).Value;
        var command = new UpdateDoctorCommand(
            DoctorId: Guid.NewGuid(),
            FirstName: "Saeed",
            LastName: "Mhd",
            Specialization: "Orthodontics",
            ContactInfo: contact,
            Gender: Gender.Male);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains(result.Errors, e => e.Code == "Doctor.NotFound" || e.Description.Contains("not found", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Handle_WithDomainValidationError_ShouldFail()
    {
        // Arrange
        var initialContact = ContactInfo.Create("+963911111111", null, true, null).Value;
        var doctor = Doctor.Create("InitialFirst", "InitialLast", "General", initialContact, Gender.Male).Value;

        await _context.Doctors.AddAsync(doctor);
        await _context.SaveChangesAsync(default);

        // إرسال قيمة غير صالحة للـ Domain (مثلاً اسم فارغ يرفضه الـ Domain Update method)
        var command = new UpdateDoctorCommand(
            DoctorId: doctor.Id,
            FirstName: string.Empty,
            LastName: "UpdatedLast",
            Specialization: "Orthodontics",
            ContactInfo: initialContact,
            Gender: Gender.Male);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
    }
}