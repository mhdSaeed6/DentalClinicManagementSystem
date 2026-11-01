using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.TreatmentPlans.Dtos;
using DentalClinic.Application.Features.TreatmentPlans.Mappers;
using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.TreatmentPlans.Commands.PerformTreatmentPlanItem;

public class PerformTreatmentPlanItemCommandHandler(
    ILogger<PerformTreatmentPlanItemCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache,
    IUser user)
    : IRequestHandler<PerformTreatmentPlanItemCommand, Result<TreatmentPlanItemDto>>
{
    public async Task<Result<TreatmentPlanItemDto>> Handle(PerformTreatmentPlanItemCommand request, CancellationToken cancellationToken)
    {
        var plan = await context.TreatmentPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken);
        if (plan is null)
        {
            logger.LogWarning("Plan item performance failed. Plan {PlanId} not found.", request.PlanId);
            return ApplicationErrors.TreatmentPlanNotFound;
        }

        if (!user.CanWriteClinicalRecords() || !user.CanAccessAppointment(plan.DoctorId))
        {
            logger.LogWarning("Plan {PlanId} item performance denied for user {UserId}.", request.PlanId, user.Id);
            return user.CanWriteClinicalRecords()
                ? ApplicationErrors.TreatmentPlanAccessForbidden
                : ApplicationErrors.ClinicalWriteForbidden;
        }

        if (request.AppointmentId.HasValue)
        {
            var appointment = await context.Appointments
                .AsNoTracking()
                .Where(a => a.Id == request.AppointmentId.Value)
                .Select(a => new { a.PatientId, a.DoctorId })
                .FirstOrDefaultAsync(cancellationToken);
            if (appointment is null)
            {
                logger.LogWarning("Plan item performance failed. Appointment {AppointmentId} not found.", request.AppointmentId);
                return ApplicationErrors.AppointmentNotFound;
            }

            // A plan item may only be attached to an appointment of the same
            // patient and doctor, otherwise its cost would be billed on the
            // wrong patient's invoice.
            if (appointment.PatientId != plan.PatientId || appointment.DoctorId != plan.DoctorId)
            {
                logger.LogWarning(
                    "Plan item performance failed. Appointment {AppointmentId} does not match plan {PlanId}.",
                    request.AppointmentId,
                    request.PlanId);
                return ApplicationErrors.TreatmentPlanItemAppointmentMismatch;
            }
        }

        var item = await context.TreatmentRecords
            .FirstOrDefaultAsync(r => r.Id == request.ItemId && r.PlanId == request.PlanId, cancellationToken);
        if (item is null)
        {
            logger.LogWarning("Plan {PlanId} item performance failed. Item {ItemId} not found.", request.PlanId, request.ItemId);
            return ApplicationErrors.TreatmentPlanItemNotFound;
        }

        var result = item.MarkPerformed(request.ActualCost, request.AppointmentId);
        if (result.IsError)
        {
            logger.LogWarning("Failed to perform plan item {ItemId}. Errors: {@Errors}", request.ItemId, result.Errors);
            return result.Errors;
        }

        await context.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync("treatment-plan", cancellationToken);
        await cache.RemoveByTagAsync("treatment-record", cancellationToken);

        return item.ToPlanItemDto();
    }
}