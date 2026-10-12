using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Identity.Dtos;
using DentalClinic.Application.Features.Identity.Queries.GetUserInfo;
using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.Identity.Queries.GetUserInfo;

public class GetUserByIdQueryHanlder(
    ILogger<GetUserByIdQueryHanlder> logger,
    IIdentityService identityService,
    IAppDbContext context)
    : IRequestHandler<GetUserByIdQuery, Result<AppUserDto>>
{
    private readonly ILogger<GetUserByIdQueryHanlder> _logger = logger;
    private readonly IIdentityService _identityService = identityService;
    private readonly IAppDbContext _context = context;

    public async Task<Result<AppUserDto>> Handle(GetUserByIdQuery request, CancellationToken ct)
    {
        var getUserByIdResult = await _identityService.GetUserByIdAsync(request.UserId!);

        if (getUserByIdResult.IsError)
        {
            _logger.LogError("User with Id { UserId }{ErrorDetails}", request.UserId, getUserByIdResult.TopError.Description);

            return getUserByIdResult.Errors;
        }

        var user = getUserByIdResult.Value;

        // Resolve the linked doctor profile (if any) so doctor-facing clients can
        // scope their views to the signed-in clinician.
        var doctorId = await _context.Doctors
            .Where(d => d.UserId == user.UserId)
            .Select(d => (Guid?)d.Id)
            .FirstOrDefaultAsync(ct);

        return user with { DoctorId = doctorId };
    }
}