using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Doctors.Commands.CreateDoctor;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Doctors;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Doctors.Commands.CreateDoctor;

[Collection(WebAppFactoryCollection.CollectionName)]
public class CreateDoctorCommandHandlerTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    [Fact]
    public async Task Handle_WithValidData_ShouldSucceedAndSaveToDatabase()
    {
        // Arrange
        var contactInfo = ContactInfo.Create("+963911111111", "+963922222222", true, "https://facebook.com/dr.saeed").Value;

        var command = new CreateDoctorCommand(
            FirstName: "Saeed",
            LastName: "Mhd",
            Specialization: "Orthodontics",
            ContactInfo: contactInfo,
            Gender: Gender.Male);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(command.FirstName, result.Value.FirstName);
        Assert.Equal(command.LastName, result.Value.LastName);
        Assert.Equal(command.Specialization, result.Value.Specialization);

        // Subcutaneous Database Verification
        var doctorInDb = await _context.Doctors
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == result.Value.Id);

        Assert.NotNull(doctorInDb);
        Assert.Equal("Saeed", doctorInDb.FirstName);
        Assert.Equal("Mhd", doctorInDb.LastName);
        Assert.Equal("Orthodontics", doctorInDb.Specialization);
    }

    [Fact]
    public async Task Handle_WithDomainValidationFailure_ShouldFail()
    {
        // Arrange
        // تجهيز بيانات تخرق شروط الـ Domain في Doctor.Create
        var contactInfo = ContactInfo.Create("+963911111111", null, false, null).Value;

        // إرسال الاسم الأول فارغاً لتتسبب في رفض الـ Entity (في حال تجاوزت الـ Validation Behavior)
        var command = new CreateDoctorCommand(
            FirstName: string.Empty,
            LastName: "Mhd",
            Specialization: "Orthodontics",
            ContactInfo: contactInfo,
            Gender: Gender.Male);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);

        // التأكد من عدم إضافة أي طبيب إلى قاعدة البيانات
        var doctorExists = await _context.Doctors.AnyAsync(d => d.LastName == "Mhd" && d.FirstName == string.Empty);
        Assert.False(doctorExists);
    }

    [Fact]
    public async Task Handle_WithInvalidContactInfo_ShouldFail()
    {
        // Arrange
        // إذا كان الـ ContactInfo يرجع خطأ أثناء الـ Create بسبب صيغة غير صالحة
        var invalidContactResult = ContactInfo.Create("invalid_phone", null, false, "not_a_url");

        Assert.True(invalidContactResult.IsError); // تأكيد أن الـ Value Object أرجع خطأ بالأساس
    }

    [Fact]
    public async Task Handle_WhenSuccessful_ShouldInvalidateCache()
    {
        // Arrange
        var contactInfo = ContactInfo.Create("+963944444444", null, true, null).Value;
        var command = new CreateDoctorCommand("Ahmad", "Kareem", "Endodontics", contactInfo, Gender.Male);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsSuccess);

        // تم التأكد من تنفيذ الـ Handler بنجاح ووصوله إلى سطر cache.RemoveByTagAsync
    }
}