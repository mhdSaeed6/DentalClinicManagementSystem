using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.LabCases.Dtos;
using DentalClinic.Application.Features.LabCases.Mappers;
using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.LabCases.Commands.ChangeLabCaseStatus;

public class ChangeLabCaseStatusCommandHandler(
    ILogger<ChangeLabCaseStatusCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache,
    IUser user)
    : IRequestHandler<ChangeLabCaseStatusCommand, Result<LabCaseDto>>
{
    public async Task<Result<LabCaseDto>> Handle(ChangeLabCaseStatusCommand request, CancellationToken cancellationToken)
    {
        var labCase = await context.LabCases
            .FirstOrDefaultAsync(c => c.Id == request.LabCaseId, cancellationToken);

        if (labCase is null)
        {
            logger.LogWarning("Lab case status change failed. Case {LabCaseId} not found.", request.LabCaseId);
            return ApplicationErrors.LabCaseNotFound;
        }

        if (!user.CanWriteClinicalRecords() || !user.CanAccessAppointment(labCase.DoctorId))
        {
            logger.LogWarning("Lab case status change denied for user {UserId} on case {LabCaseId}.", user.Id, request.LabCaseId);
            return user.CanWriteClinicalRecords()
                ? ApplicationErrors.LabCaseAccessForbidden
                : ApplicationErrors.ClinicalWriteForbidden;
        }

        var transition = labCase.ChangeStatus(request.NewStatus, request.OccurredAtUtc);

        if (transition.IsError)
        {
            logger.LogWarning(
                "Invalid lab-case status transition for {LabCaseId} to {NewStatus}. Errors: {@Errors}",
                request.LabCaseId,
                request.NewStatus,
                transition.Errors);
            return transition.Errors;
        }

        await context.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync("lab-case", cancellationToken);

        var patientName = await context.Patients
            .Where(p => p.Id == labCase.PatientId)
            .Select(p => p.FirstName + " " + p.LastName)
            .FirstOrDefaultAsync(cancellationToken);

        var doctorName = await context.Doctors
            .Where(d => d.Id == labCase.DoctorId)
            .Select(d => d.FirstName + " " + d.LastName)
            .FirstOrDefaultAsync(cancellationToken);

        logger.LogInformation("Lab case {LabCaseId} moved to {Status}.", labCase.Id, labCase.Status);

        return labCase.ToDto(DateTimeOffset.UtcNow, patientName, doctorName);
    }
}