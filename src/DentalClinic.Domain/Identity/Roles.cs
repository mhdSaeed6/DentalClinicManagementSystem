namespace DentalClinic.Domain.Identity;

/// <summary>
/// Canonical role names shared by the API authorization policies and the
/// application-layer authorization checks. Keep in sync with the Identity seeder.
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Doctor = "Doctor";
    public const string Receptionist = "Receptionist";
}
