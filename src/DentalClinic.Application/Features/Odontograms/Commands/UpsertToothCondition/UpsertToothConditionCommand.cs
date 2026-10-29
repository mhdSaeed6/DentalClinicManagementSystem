using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.Odontograms.Dtos;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.Odontograms.Commands.UpsertToothCondition;

/// <summary>
/// Sets the live condition row for one tooth of a patient: a live row is updated,
/// otherwise a new one is created (stored Healthy is an explicit row, plan Q4).
/// <para>
/// <paramref name="Scope"/> is the trusted, server-derived patient scope
/// (<c>IUser.PatientScope()</c>) — never client input. The controller is the only
/// producer, exactly as for the odontogram read query.
/// </para>
/// </summary>
public sealed record UpsertToothConditionCommand(
    Guid PatientId,
    int ToothNumber,
    ToothCondition Condition,
    ToothSurface? Surface,
    string? Notes,
    PatientAccessScope Scope) : IRequest<Result<PatientToothSummaryDto>>;