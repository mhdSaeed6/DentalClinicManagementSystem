using Asp.Versioning;

using DentalClinic.Application.Features.Clinics.Commands.UpdateClinicSettings;
using DentalClinic.Application.Features.Clinics.Dtos;
using DentalClinic.Application.Features.Clinics.Queries.GetClinicSettings;
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
}