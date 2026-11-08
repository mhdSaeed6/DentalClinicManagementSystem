using DentalClinic.Domain.Identity;

namespace DentalClinic.Application.Features.Staff;

/// <summary>
/// Shared helpers for the staff-administration feature. Only the three seeded
/// roles may be granted through the management endpoints; Identity roles created
/// outside the clinic vocabulary must never leak in through a request.
/// </summary>
public static class StaffUsers
{
    public static bool IsKnownRole(string role) =>
        role == Roles.Admin || role == Roles.Doctor || role == Roles.Receptionist;
}