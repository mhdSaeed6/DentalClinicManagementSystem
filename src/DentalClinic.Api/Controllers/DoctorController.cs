using Asp.Versioning;

using DentalClinic.Application.Features.Doctors.Dtos;
using DentalClinic.Application.Features.Doctors.Queries.GetDoctors;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace DentalClinic.Api.Controllers;

[Route("api/v{version:apiVersion}/customers")]
[ApiVersion("1.0")]
// [Authorize]
public class DoctorController(ISender sender) : ApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(List<DoctorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Retrieves a list of doctors.")]
    [EndpointDescription("This endpoint retrieves a list of doctors from the system.")]
    [EndpointName("GetDoctors")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    [OutputCache(Duration = 60)]
    public async Task<IActionResult> GetDoctors(CancellationToken ct)
    {
        var result = await sender.Send(new GetDoctorsQuery(), ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }
}