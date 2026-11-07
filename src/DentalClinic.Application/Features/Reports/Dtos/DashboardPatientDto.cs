namespace DentalClinic.Application.Features.Reports.Dtos;

/// <summary>
/// One patient as a dashboard/report row: the newest registrations, with the
/// age and phone the list row shows.
/// </summary>
public sealed record DashboardPatientDto(
    Guid Id,
    string FullName,
    DateTime DateOfBirth,
    string? PrimaryPhone);