using DentalClinic.Domain.Labs;

namespace DentalClinic.Application.Features.LabCases.Dtos;

/// <summary>
/// A dental-lab case as returned by the API. <c>IsOverdue</c> is derived at
/// query time from the expected return date and the case status — an open case
/// whose expected return has passed; returned and failed cases are never
/// overdue.
/// </summary>
public sealed record LabCaseDto(
    Guid Id,
    Guid PatientId,
    string? PatientName,
    Guid DoctorId,
    string? DoctorName,
    Guid? TreatmentRecordId,
    int? ToothNumber,
    string LabName,
    string CaseType,
    LabCaseStatus Status,
    DateTimeOffset SentAtUtc,
    DateTimeOffset ExpectedReturnAtUtc,
    DateTimeOffset? ReturnedAtUtc,
    decimal Cost,
    string? Notes,
    bool IsOverdue);