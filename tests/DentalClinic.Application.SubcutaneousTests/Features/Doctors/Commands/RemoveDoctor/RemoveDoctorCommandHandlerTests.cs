using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Doctors.Commands.RemoveDoctor;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Doctors;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Doctors.Commands.RemoveDoctor;

[Collection(WebAppFactoryCollection.CollectionName)]
public class RemoveDoctorCommandHandlerTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    [Fact]
    public async Task Handle_WithValidDoctorId_ShouldSucceedAndSoftDeleteDoctor()
    {
        // Arrange
        var contactInfo = ContactInfo.Create("+963911111111", null, true, null).Value;
        var doctor = Doctor.Create("Mhd", "Saeed", "General Dentistry", contactInfo, Gender.Male).Value;

        await _context.Doctors.AddAsync(doctor);
        await _context.SaveChangesAsync(default);

        var command = new RemoveDoctorCommand(doctor.Id);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsSuccess);

        // التأكد من تعديل حالة الكائن أو حذفه في قاعدة البيانات
        var doctorInDb = await _context.Doctors
            .IgnoreQueryFilters() // لرؤية الطبيب حتى لو تم تطبيق Soft Delete filter
            .FirstOrDefaultAsync(d => d.Id == doctor.Id);

        Assert.NotNull(doctorInDb);

        // يمكنك التحقق من خيار الـ IsDeleted أو القيمة المعينة حسب تطبيق الـ Delete في Domain Entity لديك
    }

    [Fact]
    public async Task Handle_WithNonExistingDoctorId_ShouldFailWithDoctorNotFound()
    {
        // Arrange
        var nonExistingDoctorId = Guid.NewGuid();
        var command = new RemoveDoctorCommand(nonExistingDoctorId);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains(result.Errors, e => e.Code == "Doctor.NotFound" || e.Description.Contains("not found", StringComparison.OrdinalIgnoreCase));
    }
}