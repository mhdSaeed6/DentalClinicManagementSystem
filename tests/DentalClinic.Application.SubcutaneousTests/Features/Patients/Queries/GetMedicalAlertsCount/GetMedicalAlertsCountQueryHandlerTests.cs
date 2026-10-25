using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.Patients.Queries.GetMedicalAlertsCount;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Appointments;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Doctors;
using DentalClinic.Domain.Patients;
using DentalClinic.Domain.Services;

using MediatR;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Patients.Queries.GetMedicalAlertsCount;

[Collection(WebAppFactoryCollection.CollectionName)]
public class GetMedicalAlertsCountQueryHandlerTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    [Fact]
    public async Task Handle_ShouldCountOnlyPatientsWithAlertFlags()
    {
        // Arrange: one clean patient, one with a condition and one with allergies.
        // كل مريض يحتاج نسخة ContactInfo خاصّة به: مشاركة النسخة نفسها بين
        // مالكيْن من النوع نفسه يجعل EF يُرسل أعمدة النسخة المشتركة NULL للمالك الثاني.
        // الجدّة التجريبية (Demo seed) تُنشئ مرضى لديهم تنبيهات بالفعل، لذا
        // نعتمد الفَرق (Delta) لا العدد المطلق، ونفرّغ الوسم "patient" بين
        // الطلَبين لأن الاستعلام مُخزَّن مؤقتاً (ICachedQuery).
        var cache = factory.Services.CreateScope()
            .ServiceProvider.GetRequiredService<HybridCache>();
        var baseline = (await _mediator.Send(new GetMedicalAlertsCountQuery(PatientAccessScope.None))).Value;

        var clean = Patient.Create("Samir", "Qasim",
            ContactInfo.Create("+963911111111", null, true, null).Value,
            DateTime.UtcNow.AddYears(-30), Gender.Male).Value;
        var diabetic = Patient.Create("Amal", "Ziad",
            ContactInfo.Create("+963922222222", null, true, null).Value,
            DateTime.UtcNow.AddYears(-45), Gender.Female,
            new MedicalHistory { HasDiabetes = true }).Value;
        var allergic = Patient.Create("Nada", "Wadi",
            ContactInfo.Create("+963933333333", null, true, null).Value,
            DateTime.UtcNow.AddYears(-25), Gender.Female,
            new MedicalHistory { Allergies = "Penicillin" }).Value;

        await _context.Patients.AddRangeAsync(clean, diabetic, allergic);
        await _context.SaveChangesAsync(default);
        await cache.RemoveByTagAsync("patient");

        var query = new GetMedicalAlertsCountQuery(PatientAccessScope.None);

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(baseline + 2, result.Value);
    }

    [Fact]
    public async Task Handle_WithRestrictedDoctorScope_ShouldCountOnlyThatDoctorsPatients()
    {
        // Arrange: a flagged patient with an appointment is counted; one with
        // flags but no link to the doctor is invisible to the restricted scope.
        var contactInfo = ContactInfo.Create("+963911111111", null, true, null).Value;
        var patient = Patient.Create("Maha", "Idris", contactInfo, DateTime.UtcNow.AddYears(-28), Gender.Female,
            new MedicalHistory { HasAsthma = true }).Value;
        var outsider = Patient.Create("Wael", "Hana",
            ContactInfo.Create("+963922222222", null, true, null).Value,
            DateTime.UtcNow.AddYears(-50), Gender.Male,
            new MedicalHistory { HasHeartDisease = true }).Value;
        var doctor = Doctor.Create("Omar", "Ali", "Orthodontics",
            ContactInfo.Create("+963933333333", null, true, null).Value, Gender.Male).Value;
        var service = DentalService.Create("Teeth Cleaning", "A routine dental cleaning procedure.", 30).Value;

        await _context.Patients.AddRangeAsync(patient, outsider);
        await _context.Doctors.AddAsync(doctor);
        await _context.DentalServices.AddAsync(service);
        await _context.SaveChangesAsync(default);

        var appointment = Appointment.Create(patient.Id, doctor.Id, service.Id, DateTime.UtcNow.AddHours(1), 30, null).Value;
        await _context.Appointments.AddAsync(appointment);
        await _context.SaveChangesAsync(default);

        var query = new GetMedicalAlertsCountQuery(
            new PatientAccessScope(IsRestricted: true, DoctorId: doctor.Id, UserId: null));

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value);
    }
}
