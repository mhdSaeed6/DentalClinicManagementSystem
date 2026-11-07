using Asp.Versioning;

using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.Reports.Dtos;
using DentalClinic.Application.Features.Reports.Queries.GetDashboardSummary;
using DentalClinic.Contracts.Requests.Reports;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.Api.Controllers;

[Route("api/v{version:apiVersion}/reports")]
[ApiVersion("1.0")]
[Authorize(Policy = "StaffOnly")]
public class ReportsController(ISender sender, IUser user) : ApiController
{
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Retrieves the dashboard and report aggregates for a date window.")]
    [EndpointDescription("Computes every dashboard and first-reports figure in the database, in the clinic's timezone: practice counts, today's schedule, upcoming visits, revenue on invoices issued inside the window, cash collected inside the window, the appointment status mix, recent invoices, newest patients and per-doctor productivity. Both dates are inclusive clinic-local days; omitting them uses the clinic's last 30 days. A doctor only ever receives their own figures.")]
    [EndpointName("GetDashboardSummary")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> GetDashboardSummary(
        [FromQuery] GetDashboardSummaryRequest request,
        CancellationToken ct)
    {
        // Both scopes are derived from the authenticated identity, never from
        // the query string: a caller cannot widen their own report.
        var query = new GetDashboardSummaryQuery(
            Scope: user.DoctorScope(),
            PatientScope: user.PatientScope(),
            From: request.From,
            To: request.To);

        var result = await sender.Send(query, ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }
}