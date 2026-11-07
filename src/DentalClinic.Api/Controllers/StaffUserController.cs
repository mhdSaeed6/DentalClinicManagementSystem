using Asp.Versioning;

using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Features.Identity.Dtos;
using DentalClinic.Application.Features.Staff.Commands.CreateStaffUser;
using DentalClinic.Application.Features.Staff.Commands.ResetStaffUserPassword;
using DentalClinic.Application.Features.Staff.Commands.SetStaffUserActive;
using DentalClinic.Application.Features.Staff.Commands.UpdateStaffUserRoles;
using DentalClinic.Application.Features.Staff.Queries.GetStaffUser;
using DentalClinic.Application.Features.Staff.Queries.GetStaffUsers;
using DentalClinic.Contracts.Requests.Staff;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.Api.Controllers;

/// <summary>
/// Staff-account administration for the clinic owner. Class-level AdminOnly,
/// plus handler-level role checks inside every command — the same two-layer
/// authorization used across the clinic features.
/// </summary>
[Route("api/v{version:apiVersion}/staff/users")]
[ApiVersion("1.0")]
[Authorize(Policy = "AdminOnly")]
public sealed class StaffUserController(ISender sender) : ApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<StaffUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Retrieves staff accounts.")]
    [EndpointDescription("Lists clinic staff accounts with their roles, lockout state and linked doctor profile.")]
    [EndpointName("GetStaffUsers")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> GetStaffUsers([FromQuery] GetStaffUsersRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new GetStaffUsersQuery(
            request.SearchTerm,
            request.PageNumber,
            request.PageSize), ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpGet("{userId}")]
    [ProducesResponseType(typeof(StaffUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Retrieves a staff account.")]
    [EndpointName("GetStaffUser")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> GetStaffUser(string userId, CancellationToken ct)
    {
        var result = await sender.Send(new GetStaffUserQuery(userId), ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpPost]
    [ProducesResponseType(typeof(StaffUserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Creates a staff account.")]
    [EndpointDescription("Creates an identity account with the given roles and password, email-confirmed so the first login works immediately.")]
    [EndpointName("CreateStaffUser")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> CreateStaffUser([FromBody] CreateStaffUserRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new CreateStaffUserCommand(
            request.Email,
            request.Password,
            request.Roles), ct);

        return result.Match(
            response => CreatedAtAction(nameof(GetStaffUser), new { userId = response.UserId }, response),
            Problem);
    }

    [HttpPut("{userId}/roles")]
    [ProducesResponseType(typeof(StaffUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Replaces the roles of a staff account.")]
    [EndpointName("UpdateStaffUserRoles")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> UpdateStaffUserRoles(string userId, [FromBody] UpdateStaffUserRolesRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new UpdateStaffUserRolesCommand(
            userId,
            request.Roles), ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpPut("{userId}/status")]
    [ProducesResponseType(typeof(StaffUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Activates or deactivates a staff account.")]
    [EndpointDescription("Deactivation permanently locks the account; login failures stay uniform afterwards.")]
    [EndpointName("SetStaffUserActive")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> SetStaffUserActive(string userId, [FromBody] SetStaffUserActiveRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new SetStaffUserActiveCommand(
            userId,
            request.IsActive), ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpPut("{userId}/password")]
    [ProducesResponseType(typeof(StaffUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Resets a staff account password.")]
    [EndpointName("ResetStaffUserPassword")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> ResetStaffUserPassword(string userId, [FromBody] ResetStaffUserPasswordRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new ResetStaffUserPasswordCommand(
            userId,
            request.NewPassword), ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }
}