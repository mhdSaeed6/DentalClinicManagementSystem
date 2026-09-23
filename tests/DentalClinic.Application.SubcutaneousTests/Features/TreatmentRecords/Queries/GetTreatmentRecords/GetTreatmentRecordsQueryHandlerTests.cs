using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.TreatmentRecords.Queries.GetTreatmentRecords;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Doctors;
using DentalClinic.Domain.Patients;
using DentalClinic.Domain.TreatmentRecords;
using MediatR;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.TreatmentRecords.Queries.GetTreatmentRecords;

[Collection(WebAppFactoryCollection.CollectionName)]
public class GetTreatmentRecordsQueryHandlerTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    [Fact]
    public async Task Handle_WithFiltersAndPagination_ShouldReturnFilteredPaginatedList()
    {
        // Arrange
        var (patient, doctor) = await SeedPatientAndDoctorAsync();

        var record1 = TreatmentRecord.Create(patient.Id, doctor.Id, 16, "Scaling & Polishing", 50, null).Value;
        var record2 = TreatmentRecord.Create(patient.Id, doctor.Id, 21, "Composite Filling", 100, null).Value;

        // سجل لمريض آخر للتأكد من عمل الفلترة
        var otherPatientContact = ContactInfo.Create("+963922222222", null, true, null).Value;
        var otherPatient = Patient.Create("Rami", "Nader", otherPatientContact, DateTime.UtcNow.AddYears(-20), Gender.Male).Value;
        await _context.Patients.AddAsync(otherPatient);
        var record3 = TreatmentRecord.Create(otherPatient.Id, doctor.Id, 11, "Extraction", 80, null).Value;

        await _context.TreatmentRecords.AddRangeAsync(record1, record2, record3);
        await _context.SaveChangesAsync(default);

        var query = new GetTreatmentRecordsQuery(
            PageNumber: 1,
            PageSize: 10,
            PatientId: patient.Id,
            DoctorId: doctor.Id);

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.Equal(2, result.Value.Items!.Count);
        Assert.All(result.Value.Items, item => Assert.Equal(patient.Id, item.PatientId));
    }

    [Fact]
    public async Task Handle_WhenNoMatchingRecords_ShouldReturnEmptyPaginatedList()
    {
        // Arrange
        var query = new GetTreatmentRecordsQuery(PageNumber: 1, PageSize: 10, PatientId: Guid.NewGuid());

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value.Items!);
        Assert.Equal(0, result.Value.TotalCount);
    }

    private async Task<(Patient Patient, Doctor Doctor)> SeedPatientAndDoctorAsync()
    {
        var contactInfo = ContactInfo.Create("+963911111111", null, true, null).Value;
        var patient = Patient.Create("Samer", "Ali", contactInfo, DateTime.UtcNow.AddYears(-30), Gender.Male).Value;
        var doctor = Doctor.Create("Khaled", "Hassan", "Orthodontics", contactInfo, Gender.Male).Value;

        await _context.Patients.AddAsync(patient);
        await _context.Doctors.AddAsync(doctor);
        await _context.SaveChangesAsync(default);

        return (patient, doctor);
    }
}