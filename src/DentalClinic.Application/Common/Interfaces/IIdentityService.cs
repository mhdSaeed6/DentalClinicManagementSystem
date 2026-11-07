using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Features.Identity.Dtos;
using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<bool> IsInRoleAsync(string userId, string role);

    Task<bool> AuthorizeAsync(string userId, string? policyName);

    Task<Result<AppUserDto>> AuthenticateAsync(string email, string password);

    Task<Result<AppUserDto>> GetUserByIdAsync(string userId);

    Task<string?> GetUserNameAsync(string userId);

    // Staff administration (surface: Features/Staff, Admin-only endpoints).
    // Every invariant that needs raw Identity account state lives behind this
    // interface — the last-active-admin guard and the lockout-as-deactivation
    // mechanism — so handlers stay free of UserManager details.
    Task<Result<PaginatedList<StaffUserDto>>> ListStaffUsersAsync(string? searchTerm, int pageNumber, int pageSize, CancellationToken ct = default);

    Task<Result<StaffUserDto>> GetStaffUserAsync(string userId, CancellationToken ct = default);

    Task<Result<StaffUserDto>> CreateStaffUserAsync(string email, string password, IReadOnlyList<string> roles, CancellationToken ct = default);

    Task<Result<StaffUserDto>> UpdateStaffUserRolesAsync(string userId, IReadOnlyList<string> roles, string? actingUserId, CancellationToken ct = default);

    Task<Result<StaffUserDto>> SetStaffUserActiveAsync(string userId, bool isActive, string? actingUserId, CancellationToken ct = default);

    Task<Result<StaffUserDto>> ResetStaffUserPasswordAsync(string userId, string newPassword, CancellationToken ct = default);
}