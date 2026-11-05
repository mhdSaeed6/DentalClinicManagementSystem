using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Clinics.Dtos;
using DentalClinic.Application.Features.Clinics.Mappers;
using DentalClinic.Domain.Clinics;
using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.Clinics.Commands.UpdateClinicSettings;

public class UpdateClinicSettingsCommandHandler(
    ILogger<UpdateClinicSettingsCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache)
    : IRequestHandler<UpdateClinicSettingsCommand, Result<ClinicSettingsDto>>
{
    public async Task<Result<ClinicSettingsDto>> Handle(
        UpdateClinicSettingsCommand request,
        CancellationToken cancellationToken)
    {
        var settings = await context.ClinicSettings
            .FirstOrDefaultAsync(cancellationToken);

        // Upsert: a fresh database has no row until the first save, and refusing
        // to create it would leave the clinic permanently unconfigurable.
        if (settings is null)
        {
            var createResult = ClinicSetting.Create(
                request.Name,
                request.NameArabic,
                request.LogoUrl,
                request.Phone,
                request.Address,
                request.Email,
                request.TimeZoneId,
                request.BaseCurrency,
                request.SecondaryCurrency,
                request.DefaultCountryCode);

            if (createResult.IsError)
            {
                logger.LogWarning(
                    "Clinic settings creation rejected: {@Errors}",
                    createResult.Errors);

                return createResult.Errors;
            }

            settings = createResult.Value;

            context.ClinicSettings.Add(settings);
        }
        else
        {
            var updateResult = settings.Update(
                request.Name,
                request.NameArabic,
                request.LogoUrl,
                request.Phone,
                request.Address,
                request.Email,
                request.TimeZoneId,
                request.BaseCurrency,
                request.SecondaryCurrency,
                request.DefaultCountryCode);

            if (updateResult.IsError)
            {
                logger.LogWarning(
                    "Clinic settings update rejected: {@Errors}",
                    updateResult.Errors);

                return updateResult.Errors;
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        // Branding and timezone are cached everywhere; one tag clears them all.
        await cache.RemoveByTagAsync("clinic-setting", cancellationToken);

        logger.LogInformation("Clinic settings saved.");

        return settings.ToDto();
    }
}