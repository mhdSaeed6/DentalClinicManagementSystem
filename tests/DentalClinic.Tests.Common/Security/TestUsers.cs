using DentalClinic.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace DentalClinic.Tests.Common.Security;

public static class TestUsers
{
    // These satisfy the Identity password policy configured in
    // Infrastructure/DependencyInjection.cs (12+ chars, upper, lower, digit,
    // non-alphanumeric, 4+ unique chars).
    public const string AdminPassword = "Admin!Pass123";
    public const string DoctorPassword = "Doctor!Pass123";
    public const string ReceptionistPassword = "Receptionist!Pass123";

    private static readonly PasswordHasher<AppUser> _hasher = new();

    public static AppUser Admin => new()
    {
        Id = "19a59129-6c20-417a-834d-11a208d32d96",
        Email = "admin@dentalclinic.com",
        UserName = "admin@dentalclinic.com",
        EmailConfirmed = true
    };

    public static AppUser Doctor => new()
    {
        Id = "d0c7a5b2-3f61-4c8e-9a2d-1b2c3d4e5f60",
        Email = "doctor.tests@dentalclinic.com",
        UserName = "doctor.tests@dentalclinic.com",
        EmailConfirmed = true
    };

    public static AppUser Receptionist => new()
    {
        Id = "a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d",
        Email = "receptionist.tests@dentalclinic.com",
        UserName = "receptionist.tests@dentalclinic.com",
        EmailConfirmed = true
    };

    /// <summary>
    /// Produces a user whose password hash is set locally, useful for scenarios
    /// where the account is not created through the API seeder.
    /// </summary>
    public static AppUser WithPassword(AppUser user, string password)
    {
        user.PasswordHash = _hasher.HashPassword(user, password);
        return user;
    }
}
