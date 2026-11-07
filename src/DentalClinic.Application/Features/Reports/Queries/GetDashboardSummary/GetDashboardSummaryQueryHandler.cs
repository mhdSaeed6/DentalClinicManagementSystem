using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Common.Time;
using DentalClinic.Application.Features.Invoices.Queries.GetInvoiceSummary;
using DentalClinic.Application.Features.Reports.Dtos;
using DentalClinic.Domain.Appointments;
using DentalClinic.Domain.Appointments.Enums;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Invoices;
using DentalClinic.Domain.Patients;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace DentalClinic.Application.Features.Reports.Queries.GetDashboardSummary;

/// <summary>
/// Computes the dashboard and the first reports page in SQL, over the whole
/// dataset, in the clinic's timezone.
/// </summary>
/// <remarks>
/// Two rules keep this honest: every list is bounded by a <c>Take</c> (so the
/// payload is a dashboard, not a dump), and every figure is a SQL aggregate over
/// the scoped set (so the numbers do not depend on how many rows happened to fit
/// in a page). The money maths is delegated to
/// <see cref="InvoiceSummaryReader"/> so the reports can never disagree with the
/// billing screen.
/// </remarks>
public class GetDashboardSummaryQueryHandler(IAppDbContext context, TimeProvider timeProvider)
    : IRequestHandler<GetDashboardSummaryQuery, Result<DashboardSummaryDto>>
{
    /// <summary>How many "next visits" the dashboard panel shows.</summary>
    private const int UpcomingAppointmentLimit = 6;

    /// <summary>How many of today's appointments the schedule list carries.</summary>
    private const int TodayAppointmentLimit = 12;

    private const int RecentInvoiceLimit = 5;

    private const int NewestPatientLimit = 5;

    /// <summary>Register #7: the default window is the clinic's last 30 days, inclusive.</summary>
    private const int DefaultReportDays = 30;

    /// <summary>
    /// Front-desk grace: an appointment that started up to an hour ago and is
    /// still marked scheduled stays visible, because nobody has closed it yet.
    /// </summary>
    private static readonly TimeSpan UpcomingGrace = TimeSpan.FromHours(1);

    public async Task<Result<DashboardSummaryDto>> Handle(
        GetDashboardSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var timeZone = await ClinicClock.ResolveTimeZoneAsync(context, cancellationToken);
        var nowUtc = timeProvider.GetUtcNow();
        var today = ClinicClock.LocalDate(nowUtc, timeZone);

        var to = request.To ?? today;
        var from = request.From ?? to.AddDays(-(DefaultReportDays - 1));

        // The validator rejects this too; the guard keeps a direct handler call
        // (and the unit tests) from silently producing an inverted window.
        if (to < from)
        {
            return ApplicationErrors.InvalidReportRange;
        }

        var (fromUtc, toUtcExclusive) = ClinicClock.UtcWindow(from, to, timeZone);
        var (todayStartUtc, todayEndUtc) = ClinicClock.UtcWindow(today, today, timeZone);

        // Every appointment figure is narrowed by the trusted doctor scope
        // before any date filter, exactly like the appointments list.
        var appointments = context.Appointments.AsNoTracking();

        if (request.Scope.IsRestricted)
        {
            var scopeDoctorId = request.Scope.DoctorId;
            appointments = appointments.Where(appointment => appointment.DoctorId == scopeDoctorId);
        }

        var appointmentsInRange = appointments.Where(appointment =>
            appointment.ScheduledDateTime >= fromUtc.UtcDateTime &&
            appointment.ScheduledDateTime < toUtcExclusive.UtcDateTime);

        var todaysAppointments = appointments.Where(appointment =>
            appointment.ScheduledDateTime >= todayStartUtc.UtcDateTime &&
            appointment.ScheduledDateTime < todayEndUtc.UtcDateTime);

        // Practice size: real counts, never the length of a page.
        var totalPatients = await context.Patients
            .AsNoTracking()
            .ApplyScope(context, request.PatientScope)
            .CountAsync(cancellationToken);

        var patientsWithMedicalAlerts = await context.Patients
            .AsNoTracking()
            .ApplyScope(context, request.PatientScope)
            .Where(PatientAlerts.Predicate)
            .CountAsync(cancellationToken);

        var totalDoctors = await context.Doctors.AsNoTracking().CountAsync(cancellationToken);
        var totalServices = await context.DentalServices.AsNoTracking().CountAsync(cancellationToken);

        var todayAppointmentCount = await todaysAppointments.CountAsync(cancellationToken);
        var todayScheduledCount = await todaysAppointments
            .CountAsync(appointment => appointment.Status == AppointmentStatus.Scheduled, cancellationToken);

        // Status mix over the window, zero-filled so the UI never invents a bar.
        var statusCounts = await appointmentsInRange
            .GroupBy(appointment => appointment.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var statusBreakdown = Enum.GetValues<AppointmentStatus>()
            .Select(status => new AppointmentStatusCountDto(
                status,
                statusCounts.FirstOrDefault(row => row.Status == status)?.Count ?? 0))
            .ToList();

        var todayAppointments = await ReadAppointmentsAsync(
            todaysAppointments,
            TodayAppointmentLimit,
            cancellationToken);

        var upcomingFromUtc = nowUtc.UtcDateTime - UpcomingGrace;
        var upcomingAppointments = await ReadAppointmentsAsync(
            appointments.Where(appointment =>
                appointment.Status == AppointmentStatus.Scheduled &&
                appointment.ScheduledDateTime >= upcomingFromUtc),
            UpcomingAppointmentLimit,
            cancellationToken);

        // Revenue is the invoice cohort issued in the window; the money maths is
        // the billing screen's, not a second implementation.
        var issuedInRange = InvoiceSummaryReader
            .Issued(context, request.Scope)
            .Where(invoice =>
                invoice.IssuedAtUtc >= fromUtc &&
                invoice.IssuedAtUtc < toUtcExclusive);

        var revenue = await InvoiceSummaryReader.ReadAsync(issuedInRange, cancellationToken);

        // The balance on the books is not window-scoped: debt from an earlier
        // month is still owed today.
        var outstanding = await InvoiceSummaryReader.ReadAsync(
            InvoiceSummaryReader.Issued(context, request.Scope),
            cancellationToken);

        // Cash is the different question: payments received in the window,
        // whatever invoice they settled.
        var payments = context.Payments.AsNoTracking().Where(payment =>
            payment.PaidAtUtc >= fromUtc.UtcDateTime &&
            payment.PaidAtUtc < toUtcExclusive.UtcDateTime);

        if (request.Scope.IsRestricted)
        {
            var scopedInvoiceIds = InvoiceSummaryReader
                .Scoped(context, request.Scope)
                .Select(invoice => invoice.Id);

            payments = payments.Where(payment => scopedInvoiceIds.Contains(payment.InvoiceId));
        }

        var cashCollected = await payments.SumAsync(payment => payment.Amount, cancellationToken);

        var recentInvoices = await ReadRecentInvoicesAsync(request.Scope, cancellationToken);
        var newestPatients = await ReadNewestPatientsAsync(request.PatientScope, cancellationToken);
        var doctorProductivity = await ReadProductivityAsync(
            appointmentsInRange,
            issuedInRange,
            cancellationToken);

        return new DashboardSummaryDto(
            TotalPatients: totalPatients,
            TotalDoctors: totalDoctors,
            TotalServices: totalServices,
            PatientsWithMedicalAlerts: patientsWithMedicalAlerts,
            Today: today,
            TodayAppointmentCount: todayAppointmentCount,
            TodayScheduledCount: todayScheduledCount,
            TodayAppointments: todayAppointments,
            From: from,
            To: to,
            Revenue: revenue,
            Outstanding: outstanding,
            CashCollected: cashCollected,
            AppointmentStatusBreakdown: statusBreakdown,
            UpcomingAppointments: upcomingAppointments,
            RecentInvoices: recentInvoices,
            NewestPatients: newestPatients,
            DoctorProductivity: doctorProductivity);
    }

    /// <summary>
    /// Projects a bounded, ordered set of appointments and resolves the patient,
    /// doctor and service names in one extra lookup each. Names come back empty
    /// when a row is missing, so the UI can localise its own fallback label.
    /// </summary>
    private async Task<IReadOnlyList<DashboardAppointmentDto>> ReadAppointmentsAsync(
        IQueryable<Appointment> query,
        int take,
        CancellationToken cancellationToken)
    {
        var rows = await query
            .OrderBy(appointment => appointment.ScheduledDateTime)
            .Take(take)
            .Select(appointment => new
            {
                appointment.Id,
                appointment.PatientId,
                appointment.DoctorId,
                appointment.ServiceId,
                appointment.ScheduledDateTime,
                appointment.DurationInMinutes,
                appointment.Status,
            })
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return [];
        }

        var patientIds = rows.Select(row => row.PatientId).Distinct().ToList();
        var doctorIds = rows.Select(row => row.DoctorId).Distinct().ToList();
        var serviceIds = rows
            .Where(row => row.ServiceId.HasValue)
            .Select(row => row.ServiceId!.Value)
            .Distinct()
            .ToList();

        var patientNames = await context.Patients
            .AsNoTracking()
            .Where(patient => patientIds.Contains(patient.Id))
            .Select(patient => new { patient.Id, Name = patient.FirstName + " " + patient.LastName })
            .ToDictionaryAsync(row => row.Id, row => row.Name, cancellationToken);

        var doctorNames = await context.Doctors
            .AsNoTracking()
            .Where(doctor => doctorIds.Contains(doctor.Id))
            .Select(doctor => new { doctor.Id, Name = doctor.FirstName + " " + doctor.LastName })
            .ToDictionaryAsync(row => row.Id, row => row.Name, cancellationToken);

        var serviceNames = new Dictionary<Guid, string>();

        if (serviceIds.Count > 0)
        {
            serviceNames = await context.DentalServices
                .AsNoTracking()
                .Where(service => serviceIds.Contains(service.Id))
                .Select(service => new { service.Id, service.Name })
                .ToDictionaryAsync(row => row.Id, row => row.Name, cancellationToken);
        }

        return rows
            .Select(row => new DashboardAppointmentDto(
                row.Id,
                row.PatientId,
                patientNames.GetValueOrDefault(row.PatientId, string.Empty),
                row.DoctorId,
                doctorNames.GetValueOrDefault(row.DoctorId, string.Empty),
                row.ServiceId is { } serviceId ? serviceNames.GetValueOrDefault(serviceId) : null,
                row.ScheduledDateTime,
                row.DurationInMinutes,
                row.Status))
            .ToList();
    }

    /// <summary>
    /// The newest invoices the caller may see, drafts included — they are work
    /// in progress at the front desk, not noise.
    /// </summary>
    private async Task<IReadOnlyList<DashboardInvoiceDto>> ReadRecentInvoicesAsync(
        DoctorAccessScope scope,
        CancellationToken cancellationToken)
    {
        var rows = await InvoiceSummaryReader
            .Scoped(context, scope)
            .OrderByDescending(invoice => invoice.CreatedAtUtc)
            .Take(RecentInvoiceLimit)
            .Select(invoice => new
            {
                invoice.Id,
                invoice.InvoiceNumber,
                invoice.PatientId,
                invoice.TotalAmount,
                invoice.PaidAmount,
                invoice.Status,
                invoice.LifecycleStatus,
            })
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return [];
        }

        var patientIds = rows.Select(row => row.PatientId).Distinct().ToList();
        var patientNames = await context.Patients
            .AsNoTracking()
            .Where(patient => patientIds.Contains(patient.Id))
            .Select(patient => new { patient.Id, Name = patient.FirstName + " " + patient.LastName })
            .ToDictionaryAsync(row => row.Id, row => row.Name, cancellationToken);

        return rows
            .Select(row => new DashboardInvoiceDto(
                row.Id,
                row.InvoiceNumber,
                row.PatientId,
                patientNames.GetValueOrDefault(row.PatientId, string.Empty),
                row.TotalAmount,
                row.TotalAmount - row.PaidAmount,
                row.Status,
                row.LifecycleStatus))
            .ToList();
    }

    private async Task<IReadOnlyList<DashboardPatientDto>> ReadNewestPatientsAsync(
        PatientAccessScope scope,
        CancellationToken cancellationToken) =>
        await context.Patients
            .AsNoTracking()
            .ApplyScope(context, scope)
            .OrderByDescending(patient => patient.CreatedAtUtc)
            .Take(NewestPatientLimit)
            .Select(patient => new DashboardPatientDto(
                patient.Id,
                patient.FirstName + " " + patient.LastName,
                patient.DateOfBirth,
                patient.ContactInfo.PrimaryPhone))
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Appointments booked with each doctor and the invoices issued against
    /// those appointments. A standalone invoice has no owning doctor, so it
    /// stays out of every row rather than being attributed to the viewer.
    /// </summary>
    private async Task<IReadOnlyList<DoctorProductivityDto>> ReadProductivityAsync(
        IQueryable<Appointment> appointmentsInRange,
        IQueryable<Invoice> issuedInRange,
        CancellationToken cancellationToken)
    {
        var appointmentCounts = await appointmentsInRange
            .GroupBy(appointment => appointment.DoctorId)
            .Select(group => new { DoctorId = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var invoiceTotals = await issuedInRange
            .Where(invoice => invoice.AppointmentId != null)
            .Join(
                context.Appointments,
                invoice => invoice.AppointmentId,
                appointment => (Guid?)appointment.Id,
                (invoice, appointment) => new
                {
                    appointment.DoctorId,
                    invoice.TotalAmount,
                    invoice.PaidAmount,
                })
            .GroupBy(row => row.DoctorId)
            .Select(group => new
            {
                DoctorId = group.Key,
                Count = group.Count(),
                Billed = group.Sum(row => row.TotalAmount),
                Paid = group.Sum(row => row.PaidAmount),
            })
            .ToListAsync(cancellationToken);

        var doctorIds = appointmentCounts
            .Select(row => row.DoctorId)
            .Union(invoiceTotals.Select(row => row.DoctorId))
            .ToList();

        if (doctorIds.Count == 0)
        {
            return [];
        }

        var doctorNames = await context.Doctors
            .AsNoTracking()
            .Where(doctor => doctorIds.Contains(doctor.Id))
            .Select(doctor => new { doctor.Id, Name = doctor.FirstName + " " + doctor.LastName })
            .ToDictionaryAsync(row => row.Id, row => row.Name, cancellationToken);

        var appointmentsByDoctor = appointmentCounts.ToDictionary(row => row.DoctorId);
        var invoicesByDoctor = invoiceTotals.ToDictionary(row => row.DoctorId);

        return doctorIds
            .Select(doctorId => new DoctorProductivityDto(
                doctorId,
                doctorNames.GetValueOrDefault(doctorId, string.Empty),
                appointmentsByDoctor.GetValueOrDefault(doctorId)?.Count ?? 0,
                invoicesByDoctor.GetValueOrDefault(doctorId)?.Count ?? 0,
                invoicesByDoctor.GetValueOrDefault(doctorId)?.Billed ?? 0m,
                invoicesByDoctor.GetValueOrDefault(doctorId)?.Paid ?? 0m))
            .OrderByDescending(row => row.BilledAmount)
            .ThenBy(row => row.DoctorName)
            .ToList();
    }
}