using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.LabCases.Dtos;
using DentalClinic.Application.Features.LabCases.Mappers;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Labs;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.LabCases.Commands.CreateLabCase;

public class CreateLabCaseCommandHandler(
    ILogger<CreateLabCaseCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache,
    IUser user)
    : IRequestHandler<CreateLabCaseCommand, Result<LabCaseDto>>
{
    public async Task<Result<LabCaseDto>> Handle(CreateLabCaseCommand request, CancellationToken cancellationToken)
    {
        // 0. Authorization: only admins and doctors author clinical cases, and a
        //    doctor may only open a lab case for their own profile.
        if (!user.CanWriteClinicalRecords() || !user.CanAccessAppointment(request.DoctorId))
        {
            logger.LogWarning("Lab case creation denied for user {UserId} (doctor {DoctorId}).", user.Id, request.DoctorId);
            return user.CanWriteClinicalRecords()
                ? ApplicationErrors.LabCaseAccessForbidden
                : ApplicationErrors.ClinicalWriteForbidden;
        }

        var patient = await context.Patients
            .Where(p => p.Id == request.PatientId)
            .Select(p => new { p.Id, Name = p.FirstName + " " + p.LastName })
            .FirstOrDefaultAsync(cancellationToken);

        if (patient is null)
        {
            logger.LogWarning("Lab case creation failed. Patient {PatientId} not found.", request.PatientId);
            return ApplicationErrors.PatientNotFound;
        }

        var doctor = await context.Doctors
            .Where(d => d.Id == request.DoctorId)
            .Select(d => new { d.Id, Name = d.FirstName + " " + d.LastName })
            .FirstOrDefaultAsync(cancellationToken);

        if (doctor is null)
        {
            logger.LogWarning("Lab case creation failed. Doctor {DoctorId} not found.", request.DoctorId);
            return ApplicationErrors.DoctorNotFound;
        }

        if (request.TreatmentRecordId.HasValue)
        {
            var mismatch = await IsTreatmentRecordMismatchedAsync(
                context,
                request.TreatmentRecordId.Value,
                request.PatientId,
                request.DoctorId,
                cancellationToken);

            if (mismatch is not null)
            {
                logger.LogWarning(
                    "Lab case creation failed. Treatment record {TreatmentRecordId} does not belong to patient {PatientId} and doctor {DoctorId}.",
                    request.TreatmentRecordId,
                    request.PatientId,
                    request.DoctorId);
                return mismatch;
            }
        }

        var result = LabCase.Create(
            request.PatientId,
            request.DoctorId,
            request.LabName,
            request.CaseType,
            request.SentAtUtc,
            request.ExpectedReturnAtUtc,
            request.TreatmentRecordId,
            request.ToothNumber,
            request.Cost,
            request.Notes);

        if (result.IsError)
        {
            logger.LogWarning("Failed to create lab case. Errors: {@Errors}", result.Errors);
            return result.Errors;
        }

        var labCase = result.Value;

        context.LabCases.Add(labCase);
        await context.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync("lab-case", cancellationToken);

        logger.LogInformation("Lab case {LabCaseId} created for patient {PatientId}.", labCase.Id, labCase.PatientId);

        return labCase.ToDto(DateTimeOffset.UtcNow, patient.Name, doctor.Name);
    }

    /// <summary>
    /// Returns the mismatch error when <paramref name="treatmentRecordId"/> does
    /// not exist or is not attached to the same patient and doctor, otherwise
    /// <see langword="null"/>. Kept local because only the create and update
    /// handlers share this rule.
    /// </summary>
    internal static async Task<Error?> IsTreatmentRecordMismatchedAsync(
        IAppDbContext context,
        Guid treatmentRecordId,
        Guid patientId,
        Guid doctorId,
        CancellationToken cancellationToken)
    {
        var belongs = await context.TreatmentRecords.AnyAsync(
            r => r.Id == treatmentRecordId &&
                 r.PatientId == patientId &&
                 r.DoctorId == doctorId,
            cancellationToken);

        return belongs ? null : ApplicationErrors.LabCaseTreatmentRecordMismatch;
    }
}