using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Patients.Dtos;
using DentalClinic.Application.Features.Patients.Mappers;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Patients;

using MediatR;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.Patients.Commands.CreatePatient;

public class CreatePatientCommandHandler(
    ILogger<CreatePatientCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache) :
IRequestHandler<CreatePatientCommand, Result<PatientDto>>
{
    public async Task<Result<PatientDto>> Handle(CreatePatientCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating new patient: {FirstName} {LastName}", request.FirstName, request.LastName);

        var patientResult = Patient.Create(
            request.FirstName.Trim(),
            request.LastName.Trim(),
            request.ContactInfo,
            request.DateOfBirth,
            (Gender)request.Gender,
            request.MedicalHistory);

        if (patientResult.IsError)
        {
            logger.LogWarning("Failed to create patient: {FirstName} {LastName}. Errors: {@Errors}", request.FirstName,
             request.LastName, patientResult.Errors);

            return patientResult.Errors;
        }

        var patient = patientResult.Value;

        context.Patients.Add(patient);
        await context.SaveChangesAsync(cancellationToken);

        // إبطال كاش القوائم المجهزة سابقاً لضمان تحديث البيانات
        await cache.RemoveByTagAsync("patient", cancellationToken);

        logger.LogInformation("Patient created successfully with ID: {PatientId}", patient.Id);

        return patient.ToDto();
    }
}