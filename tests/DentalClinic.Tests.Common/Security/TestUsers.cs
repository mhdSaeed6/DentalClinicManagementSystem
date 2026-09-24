using DentalClinic.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace DentalClinic.Tests.Common.Security;

public static class TestUsers
{
    public const string AdminPassword = "Admin123!";
    private static readonly PasswordHasher<AppUser> _hasher = new();
    public static AppUser Admin => new()
    {
        Id = "19a59129-6c20-417a-834d-11a208d32d96",
        Email = "admin@dentalclinic.com",
        EmailConfirmed = true
    };
}