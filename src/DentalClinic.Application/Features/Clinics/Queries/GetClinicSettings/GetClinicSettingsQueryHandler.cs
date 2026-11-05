using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Clinics.Dtos;
using DentalClinic.Application.Features.Clinics.Mappers;
using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace DentalClinic.Application.Features.Clinics.Queries.GetClinicSettings;

public class GetClinicSettingsQueryHandler(IAppDbContext context)
    : IRequestHandler<GetClinicSettingsQuery, Result<ClinicSettingsDto>>
{
    public async Task<Result<ClinicSettingsDto>> Handle(
        GetClinicSettingsQuery request,
        CancellationToken cancellationToken)
    {
        var settings = await context.ClinicSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (settings is null)
        {
            return ApplicationErrors.ClinicSettingsNotConfigured;
        }

        return settings.ToDto();
    }
}