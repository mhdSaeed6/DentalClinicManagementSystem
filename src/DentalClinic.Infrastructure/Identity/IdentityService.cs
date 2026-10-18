using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Identity.Dtos;
using DentalClinic.Domain.Common.Results;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace DentalClinic.Infrastructure.Identity;

public class IdentityService(UserManager<AppUser> userManager,
                             IUserClaimsPrincipalFactory<AppUser> userClaimsPrincipalFactory,
                             IAuthorizationService authorizationService) : IIdentityService
{
    private readonly UserManager<AppUser> _userManager = userManager;
    private readonly IUserClaimsPrincipalFactory<AppUser> _userClaimsPrincipalFactory = userClaimsPrincipalFactory;
    private readonly IAuthorizationService _authorizationService = authorizationService;

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
        var user = await _userManager.FindByIdAsync(userId) ?? throw new InvalidOperationException(nameof(userId));

        var roles = await _userManager.GetRolesAsync(user);

        var claims = await _userManager.GetClaimsAsync(user);

        return new AppUserDto(user.Id, user.Email!, roles, claims);
    }

    public async Task<string?> GetUserNameAsync(string userId)
    {
         var user = await _userManager.FindByIdAsync(userId);

         return user?.UserName;
    }

    private static Error InvalidCredentials() =>
        Error.Validation("Auth_Invalid_Credentials", "Email / Password are incorrect.");
}