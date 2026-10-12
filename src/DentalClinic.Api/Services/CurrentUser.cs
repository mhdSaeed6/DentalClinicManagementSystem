using System.Security.Claims;

using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Domain.Identity;

namespace DentalClinic.Api.Services;

public class CurrentUser : IUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? Id => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public Guid? DoctorId =>
        Guid.TryParse(
            _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClinicClaimTypes.DoctorId),
            out var doctorId)
            ? doctorId
            : null;

    public bool IsInRole(string role) =>
        _httpContextAccessor.HttpContext?.User?.IsInRole(role) ?? false;
}
