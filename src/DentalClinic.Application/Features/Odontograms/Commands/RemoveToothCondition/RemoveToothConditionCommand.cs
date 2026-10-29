using DentalClinic.Application.Common.Security;
using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.Odontograms.Commands.RemoveToothCondition;

/// <summary>
/// Clears the live condition row for one tooth of a patient by soft-deleting it.
/// Removal is idempotent: when no live row exists the command simply succeeds.
/// <para>
/// <paramref name="Scope"/> is the trusted, server-derived patient scope
/// (<c>IUser.PatientScope()</c>) — never client input; same contract as the read
/// query and the upsert command.
/// </para>
/// </summary>
public sealed record RemoveToothConditionCommand(
    Guid PatientId,
    int ToothNumber,
    PatientAccessScope Scope) : IRequest<Result<Deleted>>;