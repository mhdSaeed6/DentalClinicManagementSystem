using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Common.Time;
using DentalClinic.Application.Features.Auditing.Dtos;
using DentalClinic.Application.Features.Auditing.Mappers;
using DentalClinic.Domain.Auditing;
using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace DentalClinic.Application.Features.Auditing.Queries.GetAuditLogs;

public class GetAuditLogsQueryHandler(IAppDbContext context)
    : IRequestHandler<GetAuditLogsQuery, Result<PaginatedList<AuditLogEntryDto>>>
{
    public async Task<Result<PaginatedList<AuditLogEntryDto>>> Handle(
        GetAuditLogsQuery request,
        CancellationToken cancellationToken)
    {
        var query = context.AuditLogEntries.AsNoTracking();

        // 1. Record identity. The route/UI may send the plural form, so the
        //    stored CLR name is derived through the shared normalizer.
        if (!string.IsNullOrWhiteSpace(request.EntityName) &&
            AuditEntityNames.TryNormalize(request.EntityName, out var entityName))
        {
            query = query.Where(entry => entry.EntityName == entityName);
        }

        if (request.EntityId is { } entityId && entityId != Guid.Empty)
        {
            query = query.Where(entry => entry.EntityId == entityId);
        }

        // 2. What happened. An unparseable action is rejected by the validator;
        //    the TryParse here keeps the handler total for internal callers.
        if (!string.IsNullOrWhiteSpace(request.Action) &&
            Enum.TryParse<AuditAction>(request.Action, ignoreCase: true, out var action))
        {
            query = query.Where(entry => entry.Action == action);
        }

        // 3. Who did it.
        if (!string.IsNullOrWhiteSpace(request.UserId))
        {
            var userId = request.UserId.Trim();
            query = query.Where(entry => entry.UserId == userId);
        }

        var timeZone = await ClinicClock.ResolveTimeZoneAsync(context, cancellationToken);

        // 4. When. A single supplied bound means "that clinic day"; both bounds
        //    are converted to one half-open UTC window so a 23:59 change stays
        //    on its clinic day even though UTC has already rolled over.
        if (request.From is { } from || request.To is { } to)
        {
            var (startUtc, endUtcExclusive) = ClinicClock.UtcWindow(
                request.From ?? request.To!.Value,
                request.To ?? request.From!.Value,
                timeZone);

            query = query.Where(entry =>
                entry.TimestampUtc >= startUtc && entry.TimestampUtc < endUtcExclusive);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var entries = await query
            .OrderByDescending(entry => entry.TimestampUtc)
            .ThenByDescending(entry => entry.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedList<AuditLogEntryDto>
        {
            Items = entries.Select(entry => entry.ToDto(timeZone)).ToList(),
            TotalCount = totalCount,
            TotalPages = request.PageSize > 0
                ? (int)Math.Ceiling(totalCount / (double)request.PageSize)
                : 0,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
        };
    }
}
