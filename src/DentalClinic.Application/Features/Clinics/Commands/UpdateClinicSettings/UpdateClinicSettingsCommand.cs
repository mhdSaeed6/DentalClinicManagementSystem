using DentalClinic.Application.Features.Clinics.Dtos;
using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.Clinics.Commands.UpdateClinicSettings;

public sealed record UpdateClinicSettingsCommand(
    string Name,
    string NameArabic,
    string? LogoUrl,
    string? Phone,
    string? Address,
    string? Email,
    string TimeZoneId,
    string BaseCurrency,
    string? SecondaryCurrency,
    string? DefaultCountryCode) : IRequest<Result<ClinicSettingsDto>>;