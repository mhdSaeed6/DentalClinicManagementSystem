using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Time;
using DentalClinic.Application.Features.Auditing.Dtos;
using DentalClinic.Application.Features.Auditing.Mappers;
using DentalClinic.Domain.Auditing;
using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace DentalClinic.Application.Features.Auditing.Queries.GetAuditLogsForEntity;

public class GetAuditLogsForEntityQueryHandler(IAppDbContext context)
    : IRequestHandler<GetAuditLogsForEntityQuery, Result<List<AuditLogEntryDto>>>
{
    public async Task<Result<List<AuditLogEntryDto>>> Handle(
        GetAuditLogsForEntityQuery request,
        CancellationToken cancellationToken)
    {
        // The interceptor stores the CLR type name ("Patient"). Accept the
        // route-friendly variants ("patient", "patients") and map them onto the
        // same canonical value so the history never comes back empty because of
        // casing.
        var entityName = AuditEntityNames.TryNormalize(request.EntityName, out var canonical)
            ? canonical
            : request.EntityName.Trim();

        var timeZone = await ClinicClock.ResolveTimeZoneAsync(context, cancellationToken);

        var entries = await context.AuditLogEntries
            .AsNoTracking()
            .Where(entry => entry.EntityName == entityName && entry.EntityId == request.EntityId)
            .OrderByDescending(entry => entry.TimestampUtc)
            .ToListAsync(cancellationToken);

        return entries.Select(entry => entry.ToDto(timeZone)).ToList();
    }
}
