using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Patients;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace DentalClinic.Application.Features.Patients.Queries.GetMedicalAlertsCount;

public class GetMedicalAlertsCountQueryHandler(IAppDbContext context)
    : IRequestHandler<GetMedicalAlertsCountQuery, Result<int>>
{
    public async Task<Result<int>> Handle(
        GetMedicalAlertsCountQuery request,
        CancellationToken cancellationToken)
    {
        // Same trusted, server-derived scope as the patient registry, applied
        // before the alert filter so a restricted doctor only counts their own.
        var query = context.Patients
            .AsNoTracking()
            .ApplyScope(context, request.Scope)
            .Where(PatientAlerts.Predicate);

        return await query.CountAsync(cancellationToken);
    }
}