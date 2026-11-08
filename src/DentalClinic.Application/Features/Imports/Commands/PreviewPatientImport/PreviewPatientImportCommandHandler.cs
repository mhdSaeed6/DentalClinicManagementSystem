using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Imports.Dtos;
using DentalClinic.Application.Features.Imports.Mappers;
using DentalClinic.Application.Features.Imports.Patients;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Identity;
using DentalClinic.Domain.Imports;

using MediatR;

using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.Imports.Commands.PreviewPatientImport;

public class PreviewPatientImportCommandHandler(
    ILogger<PreviewPatientImportCommandHandler> logger,
    PatientImportProcessor processor,
    IUser user)
    : IRequestHandler<PreviewPatientImportCommand, Result<PatientImportPreviewDto>>
{
    public async Task<Result<PatientImportPreviewDto>> Handle(
        PreviewPatientImportCommand request,
        CancellationToken cancellationToken)
    {
        // Bulk entry must not become a side door around the staff permission
        // model; the controller policy is the outer layer, this is the inner one.
        if (!user.IsInRole(Roles.Admin))
        {
            logger.LogWarning("Patient import preview denied for user {UserId}.", user.Id);
            return ImportErrors.AdminOnly;
        }

        var parsed = await processor.ParseAsync(
            request.FileName,
            request.Length,
            request.Content,
            request.MappingJson,
            cancellationToken);

        if (parsed.IsError)
        {
            logger.LogWarning("Patient import preview failed: {@Errors}", parsed.Errors);
            return parsed.Errors;
        }

        return parsed.Value.ToPreviewDto(PatientImportRules.PreviewRowLimit);
    }
}