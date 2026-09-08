using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Patients.Commands.UpdatePatient;
using DentalClinic.Application.Features.Patients.Dtos;
using DentalClinic.Application.Features.Patients.Mappers;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Patients;

using MediatR;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.Patients.Commands.UpdatePatient;

public class UpdatePatientCommandHandler(
    ILogger<UpdatePatientCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache) :
IRequestHandler<UpdatePatientCommand, Result<PatientDto>>
{
    public async Task<Result<PatientDto>> Handle(UpdatePatientCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating patient with ID: {PatientId}", request.PatientId);

        var patient = await context.Patients.FindAsync([request.PatientId], cancellationToken);

        if (patient is null)
        {
            logger.LogWarning("Patient update failed. Patient with ID {PatientId} not found.", request.PatientId);
            return ApplicationErrors.PatientNotFound;
        }

        var updateResult = patient.Update(
            request.FirstName.Trim(),
            request.LastName.Trim(),
            request.ContactInfo,
            request.DateOfBirth,
            (Gender)request.Gender,
            request.MedicalHistory);

        if (updateResult.IsError)
        {
            logger.LogWarning("Failed to update patient with ID {PatientId}. Errors: {@Errors}", request.PatientId, updateResult.Errors);
            return updateResult.Errors;
        }

        // الحفظ يتم أوتوماتيكياً بفضل Change Tracking الخاص بـ EF Core
        await context.SaveChangesAsync(cancellationToken);

        // إبطال كاش المريض المحدد وكاش القوائم
        await cache.RemoveAsync($"patient-{patient.Id}", cancellationToken);
        await cache.RemoveByTagAsync("patients", cancellationToken);

        logger.LogInformation("Patient updated successfully with ID: {PatientId}", patient.Id);

        return patient.ToDto();
    }
}