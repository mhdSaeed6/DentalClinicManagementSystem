using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Features.Identity.Dtos;
using DentalClinic.Application.Features.Staff;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Identity;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.Infrastructure.Identity;

public class IdentityService(UserManager<AppUser> userManager,
                             IUserClaimsPrincipalFactory<AppUser> userClaimsPrincipalFactory,
                             IAuthorizationService authorizationService,
                             IAppDbContext context) : IIdentityService
{
    private readonly UserManager<AppUser> _userManager = userManager;
    private readonly IUserClaimsPrincipalFactory<AppUser> _userClaimsPrincipalFactory = userClaimsPrincipalFactory;
    private readonly IAuthorizationService _authorizationService = authorizationService;
    private readonly IAppDbContext _context = context;

    public async Task<bool> IsInRoleAsync(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);

        return user != null && await _userManager.IsInRoleAsync(user, role);
    }

    public async Task<bool> AuthorizeAsync(string userId, string? policyName)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return false;
        }

        var principal = await _userClaimsPrincipalFactory.CreateAsync(user);

        var result = await _authorizationService.AuthorizeAsync(principal, policyName!);

        return result.Succeeded;
    }

    public async Task<Result<AppUserDto>> AuthenticateAsync(string email, string password)
    {
        // Deliberately uniform: distinguishing "no such user" from "wrong
        // password" (and "unconfirmed") turns the login endpoint into an account
        // enumeration oracle. Every failure path returns the same error.
        var user = await _userManager.FindByEmailAsync(email);

        if (user is null)
        {
            // Spend comparable time on unknown users so response latency does not
            // leak existence either.
            _ = await _userManager.FindByIdAsync(email);
            return InvalidCredentials();
        }

        if (!await _userManager.IsLockedOutAsync(user))
        {
            if (!user.EmailConfirmed)
            {
                return InvalidCredentials();
            }

            if (!await _userManager.CheckPasswordAsync(user, password))
            {
                // Counts toward the account lockout configured in AddInfrastructure.
                await _userManager.AccessFailedAsync(user);

                return InvalidCredentials();
            }

            _ = await _userManager.ResetAccessFailedCountAsync(user);
        }
        else
        {
            // Locked out: still report the same generic error.
            return InvalidCredentials();
        }

        return new AppUserDto(user.Id, user.Email!, await _userManager.GetRolesAsync(user), await _userManager.GetClaimsAsync(user));
    }

    public async Task<Result<AppUserDto>> GetUserByIdAsync(string userId)
    {
        // A missing user is an expected outcome (deleted account, stale token),
        // not a programming error: surface it as a Result so callers decide how
        // to present it instead of catching an exception.
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return ApplicationErrors.UserNotFound;
        }

        var roles = await _userManager.GetRolesAsync(user);

        var claims = await _userManager.GetClaimsAsync(user);

        return new AppUserDto(user.Id, user.Email!, roles, claims);
    }

    public async Task<string?> GetUserNameAsync(string userId)
    {
         var user = await _userManager.FindByIdAsync(userId);

         return user?.UserName;
    }

    public async Task<Result<PaginatedList<StaffUserDto>>> ListStaffUsersAsync(string? searchTerm, int pageNumber, int pageSize, CancellationToken ct = default)
    {
        var usersQuery = _userManager.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            usersQuery = usersQuery.Where(u => u.Email!.Contains(searchTerm) || u.UserName!.Contains(searchTerm));
        }

        var totalCount = await usersQuery.CountAsync(ct);

        var users = await usersQuery
            .OrderBy(u => u.Email)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        // Linked doctor profiles resolve in one round trip; each row still gets
        // its own role/lockout read because UserManager is the only accurate
        // source for both.
        var linkedDoctors = await _context.Doctors.AsNoTracking()
            .Where(d => d.UserId != null && users.Select(u => u.Id).Contains(d.UserId))
            .Select(d => new { d.UserId, d.Id, d.FirstName, d.LastName })
            .ToListAsync(ct);

        var rows = new List<StaffUserDto>(users.Count);

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var profile = linkedDoctors.FirstOrDefault(d => d.UserId == user.Id);

            rows.Add(new StaffUserDto(
                user.Id,
                user.Email!,
                user.UserName,
                roles.ToList(),
                await _userManager.IsLockedOutAsync(user))
            {
                LinkedDoctorId = profile?.Id,
                LinkedDoctorName = profile is null ? null : $"{profile.FirstName} {profile.LastName}"
            });
        }

        return new PaginatedList<StaffUserDto>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = pageSize > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 0,
            Items = rows
        };
    }

    public async Task<Result<StaffUserDto>> CreateStaffUserAsync(string email, string password, IReadOnlyList<string> roles, CancellationToken ct = default)
    {
        var user = new AppUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(user, password);

        if (!createResult.Succeeded)
        {
            return IdentityErrors(createResult);
        }

        foreach (var role in roles)
        {
            var roleResult = await _userManager.AddToRoleAsync(user, role);

            if (!roleResult.Succeeded)
            {
                return IdentityErrors(roleResult);
            }
        }

        return ToStaffUserDto(user, roles, linkedDoctor: null);
    }

    public async Task<Result<StaffUserDto>> UpdateStaffUserRolesAsync(string userId, IReadOnlyList<string> roles, string? actingUserId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return ApplicationErrors.UserNotFound;
        }

        var currentRoles = await _userManager.GetRolesAsync(user);

        var removedRoles = currentRoles.Where(role => !roles.Contains(role)).ToList();

        // Fail closed on the last active administrator: either demoting them or
        // locking their account strands the clinic with no management access.
        if (currentRoles.Contains(Roles.Admin)
            && !roles.Contains(Roles.Admin)
            && !await HasOtherActiveAdminAsync(user))
        {
            return StaffUserErrors.LastAdminProtected;
        }

        foreach (var role in removedRoles)
        {
            var removeResult = await _userManager.RemoveFromRoleAsync(user, role);

            if (!removeResult.Succeeded)
            {
                return IdentityErrors(removeResult);
            }
        }

        foreach (var role in roles.Where(role => currentRoles.All(current => current != role)))
        {
            var addResult = await _userManager.AddToRoleAsync(user, role);

            if (!addResult.Succeeded)
            {
                return IdentityErrors(addResult);
            }
        }

        return ToStaffUserDto(user, roles, await FindLinkedDoctorAsync(user.Id));
    }

    public async Task<Result<StaffUserDto>> SetStaffUserActiveAsync(string userId, bool isActive, string? actingUserId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return ApplicationErrors.UserNotFound;
        }

        if (isActive)
        {
            var unlockResult = await _userManager.SetLockoutEndDateAsync(user, null);

            if (!unlockResult.Succeeded)
            {
                return IdentityErrors(unlockResult);
            }

            _ = await _userManager.ResetAccessFailedCountAsync(user);
        }
        else
        {
            // "Deactivated" = permanently locked out. Login stays deliberately
            // uniform (invalid credentials) — no separate "account disabled"
            // error that could confirm an account exists to an attacker.
            if (await _userManager.IsInRoleAsync(user, Roles.Admin) && !await HasOtherActiveAdminAsync(user))
            {
                return StaffUserErrors.LastAdminProtected;
            }

            var lockResult = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

            if (!lockResult.Succeeded)
            {
                return IdentityErrors(lockResult);
            }
        }

        var roles = await _userManager.GetRolesAsync(user);

        return ToStaffUserDto(user, roles, await FindLinkedDoctorAsync(user.Id));
    }

    public async Task<Result<StaffUserDto>> ResetStaffUserPasswordAsync(string userId, string newPassword, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return ApplicationErrors.UserNotFound;
        }

        // AddIdentityCore registers no reset-token provider, so the
        // reset-token route is not available — the password is swapped the
        // same way the demo-account seeder does it: remove, then add. The
        // previous hash is kept so that a policy-invalid password can be
        // rolled back instead of leaving the account passwordless.
        var previousHash = user.PasswordHash;

        await _userManager.RemovePasswordAsync(user);

        var addResult = await _userManager.AddPasswordAsync(user, newPassword);

        if (!addResult.Succeeded)
        {
            user.PasswordHash = previousHash;

            await _userManager.UpdateAsync(user);

            return IdentityErrors(addResult);
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            var unlockResult = await _userManager.SetLockoutEndDateAsync(user, null);

            if (!unlockResult.Succeeded)
            {
                return IdentityErrors(unlockResult);
            }
        }

        _ = await _userManager.ResetAccessFailedCountAsync(user);

        var roles = await _userManager.GetRolesAsync(user);

        return ToStaffUserDto(user, roles, await FindLinkedDoctorAsync(user.Id));
    }

    /// <summary>
    /// Single-account read-back used by the staff screen and the POST's location
    /// header. Roles plus the lockout come from UserManager, the linked doctor
    /// from the doctors table.
    /// </summary>
    public async Task<Result<StaffUserDto>> GetStaffUserAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return ApplicationErrors.UserNotFound;
        }

        var roles = await _userManager.GetRolesAsync(user);

        return ToStaffUserDto(user, roles, await FindLinkedDoctorAsync(user.Id));
    }

    private static StaffUserDto ToStaffUserDto(AppUser user, IEnumerable<string> roles, (Guid Id, string Name)? linkedDoctor)
    {
        return new StaffUserDto(
            user.Id,
            user.Email!,
            user.UserName,
            roles.ToList(),
            user.LockoutEnd is { } lockoutEnd && lockoutEnd > DateTimeOffset.UtcNow)        {
            LinkedDoctorId = linkedDoctor?.Id,
            LinkedDoctorName = linkedDoctor?.Name
        };
    }

    private async Task<(Guid Id, string Name)?> FindLinkedDoctorAsync(string userId)
    {
        var doctor = await _context.Doctors.AsNoTracking()
            .Where(d => d.UserId == userId)
            .Select(d => new { d.Id, d.FirstName, d.LastName })
            .FirstOrDefaultAsync();

        return doctor is null ? null : (doctor.Id, $"{doctor.FirstName} {doctor.LastName}");
    }

    private async Task<bool> HasOtherActiveAdminAsync(AppUser user)
    {
        var admins = await _userManager.GetUsersInRoleAsync(Roles.Admin);

        return admins.Any(admin =>
            admin.Id != user.Id &&
            (admin.LockoutEnd is null || admin.LockoutEnd <= DateTimeOffset.UtcNow));
    }

    private static List<Error> IdentityErrors(IdentityResult result)
    {
        return result.Errors
            .Select(failure => Error.Validation($"Auth.{failure.Code}", failure.Description))
            .ToList();
    }

    private static Error InvalidCredentials() =>
        Error.Validation("Auth_Invalid_Credentials", "Email / Password are incorrect.");
}