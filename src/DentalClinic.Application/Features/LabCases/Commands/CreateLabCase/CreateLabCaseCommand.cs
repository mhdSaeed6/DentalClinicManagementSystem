using DentalClinic.Application.Features.LabCases.Dtos;
using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.LabCases.Commands.CreateLabCase;

public sealed record CreateLabCaseCommand(
    Guid PatientId,
    Guid DoctorId,
    Guid? TreatmentRecordId,
    int? ToothNumber,
    string LabName,
    string CaseType,
    DateTimeOffset SentAtUtc,
    DateTimeOffset ExpectedReturnAtUtc,
    decimal Cost,
    string? Notes) : IRequest<Result<LabCaseDto>>;