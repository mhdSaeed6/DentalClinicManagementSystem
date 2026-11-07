using DentalClinic.Application.Features.Invoices.Dtos;

namespace DentalClinic.Application.Features.Reports.Dtos;

/// <summary>
/// Everything the dashboard and the first reports page need, computed in SQL
/// over the whole dataset in the clinic's timezone.
/// </summary>
/// <remarks>
/// <para>
/// One call replaces the five "page of 100" fetches the dashboard used to
/// aggregate in the browser: counts are real <c>COUNT(*)</c> results, not the
/// length of a page (AUDIT.md D-09).
/// </para>
/// <para>
/// <see cref="Revenue"/> answers "what did we bill in this window?" and covers
/// invoices <em>issued</em> between <see cref="From"/> and <see cref="To"/>,
/// with the money they have collected so far. <see cref="CashCollected"/>
/// answers the different question "what came in?" — payments received inside
/// the same window, whatever invoice they settled. An owner needs both, and
/// conflating them makes a good month look bad.
/// </para>
/// <para>
/// <see cref="Outstanding"/> is the point-in-time balance on every issued
/// invoice, not the window's: money owed from March is still owed in November,
/// so ageing it out of the window would flatter the books.
/// </para>
/// <para>
/// Every list is bounded server-side; nothing here is a page of a larger set.
/// </para>
/// </remarks>
public sealed record DashboardSummaryDto(
    int TotalPatients,
    int TotalDoctors,
    int TotalServices,
    int PatientsWithMedicalAlerts,
    DateOnly Today,
    int TodayAppointmentCount,
    int TodayScheduledCount,
    IReadOnlyList<DashboardAppointmentDto> TodayAppointments,
    DateOnly From,
    DateOnly To,
    InvoiceSummaryDto Revenue,
    InvoiceSummaryDto Outstanding,
    decimal CashCollected,
    IReadOnlyList<AppointmentStatusCountDto> AppointmentStatusBreakdown,
    IReadOnlyList<DashboardAppointmentDto> UpcomingAppointments,
    IReadOnlyList<DashboardInvoiceDto> RecentInvoices,
    IReadOnlyList<DashboardPatientDto> NewestPatients,
    IReadOnlyList<DoctorProductivityDto> DoctorProductivity);