using DentalClinic.Application.Features.LabCases.Dtos;
using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.LabCases.Commands.UpdateLabCase;

public sealed record UpdateLabCaseCommand(
    Guid LabCaseId,
    Guid? TreatmentRecordId,
    int? ToothNumber,
    string LabName,
    string CaseType,
    DateTimeOffset ExpectedReturnAtUtc,
    decimal Cost,
    string? Notes) : IRequest<Result<LabCaseDto>>;