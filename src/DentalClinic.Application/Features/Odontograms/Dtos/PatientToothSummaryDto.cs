using DentalClinic.Domain.Common.Enums;

namespace DentalClinic.Application.Features.Odontograms.Dtos;

public sealed record PatientToothSummaryDto(
    int ToothNumber,
    ToothCondition Condition,
    ToothSurface? Surface,
    string? Notes,
    DateTimeOffset? LastModifiedUtc);