using DentalClinic.Application.Features.Auditing.Dtos;
using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.Auditing.Queries.GetAuditLogsForEntity;

/// <summary>
/// Returns the change history of one record, newest first.
///
/// Deliberately not cached: an audit trail that lags behind the write it
/// describes is worse than no trail, because it looks authoritative while being
/// stale.
/// </summary>
public sealed record GetAuditLogsForEntityQuery(string EntityName, Guid EntityId)
    : IRequest<Result<List<AuditLogEntryDto>>>;
