using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Features.Auditing.Dtos;
using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.Auditing.Queries.GetAuditLogs;

/// <summary>
/// Paged, filterable read of the whole audit trail for the administrator screen.
///
/// Deliberately <b>not</b> an <c>ICachedQuery</c>: the trail is append-only, so
/// a cached page would keep serving history that no longer matches the data as
/// soon as the next write lands. The per-record history is uncached for the
/// same reason.
/// </summary>
public sealed record GetAuditLogsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? EntityName = null,
    Guid? EntityId = null,
    string? Action = null,
    string? UserId = null,
    DateOnly? From = null,
    DateOnly? To = null) : IRequest<Result<PaginatedList<AuditLogEntryDto>>>;
