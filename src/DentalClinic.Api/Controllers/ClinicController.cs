using Asp.Versioning;

using DentalClinic.Application.Features.Clinics.Commands.AddExchangeRate;
using DentalClinic.Application.Features.Clinics.Commands.UpdateClinicSettings;
using DentalClinic.Application.Features.Clinics.Dtos;
using DentalClinic.Application.Features.Clinics.Queries.GetClinicSettings;
using DentalClinic.Application.Features.Clinics.Queries.GetExchangeRate;
using DentalClinic.Contracts.Requests.Clinics;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.Api.Controllers;

[Route("api/v{version:apiVersion}/clinic")]
[ApiVersion("1.0")]
public sealed class ClinicController(ISender sender) : ApiController
{
    /// <summary>
    /// Read the clinic profile. Anonymous on purpose: the login page and the
    /// print sheets need the clinic's name before anyone has signed in, and the
    /// payload contains nothing sensitive.
    /// </summary>
    [HttpGet("settings")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ClinicSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Retrieves the clinic profile.")]
    [EndpointDescription("Returns the clinic name, contact details, timezone and currencies. Public: the login page and printed documents need it before authentication.")]
    [EndpointName("GetClinicSettings")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> GetSettings(CancellationToken ct)
    {
        var result = await sender.Send(new GetClinicSettingsQuery(), ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    /// <summary>
    /// Update the clinic profile. Admin only: changing the timezone moves invoice
    /// year boundaries and changing the currency relabels every amount.
    /// </summary>
    [HttpPut("settings")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ClinicSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [EndpointSummary("Updates the clinic profile.")]
    [EndpointDescription("Replaces the clinic name, contact details, timezone and currency configuration. Restricted to administrators.")]
    [EndpointName("UpdateClinicSettings")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> UpdateSettings(
        [FromBody] UpdateClinicSettingsRequest request,
        CancellationToken ct)
    {
        var command = new UpdateClinicSettingsCommand(
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

        var result = await sender.Send(command, ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    /// <summary>
    /// The rate in force for the clinic's configured currency pair. Returns 404
    /// when no secondary currency or no rate is configured — the UI then hides
    /// the secondary amount rather than inventing one.
    /// </summary>
    [HttpGet("exchange-rate")]
    [Authorize(Policy = "StaffOnly")]
    [ProducesResponseType(typeof(ExchangeRateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Retrieves the exchange rate in force.")]
    [EndpointDescription("Returns the conversion rate for the clinic's base and secondary currencies at the given instant (default: now). Restricted to staff.")]
    [EndpointName("GetExchangeRate")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> GetExchangeRate(
        [FromQuery] DateTimeOffset? asOf,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetExchangeRateQuery(asOf), ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    /// <summary>
    /// Appends a rate to the history. Admin only: it changes every secondary
    /// amount the clinic displays from that instant onwards.
    /// </summary>
    [HttpPost("exchange-rates")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ExchangeRateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [EndpointSummary("Appends an exchange rate.")]
    [EndpointDescription("Adds a new row to the append-only conversion history. Existing rows are never modified, so historical printouts stay explainable. Restricted to administrators.")]
    [EndpointName("AddExchangeRate")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> AddExchangeRate(
        [FromBody] AddExchangeRateRequest request,
        CancellationToken ct)
    {
        var command = new AddExchangeRateCommand(
            request.FromCurrency,
            request.ToCurrency,
            request.Rate,
            request.EffectiveFromUtc);

        var result = await sender.Send(command, ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }
}