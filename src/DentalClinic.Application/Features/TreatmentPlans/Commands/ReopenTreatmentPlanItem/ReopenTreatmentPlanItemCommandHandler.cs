using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.TreatmentPlans.Dtos;
using DentalClinic.Application.Features.TreatmentPlans.Mappers;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Invoices.Enums;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.TreatmentPlans.Commands.ReopenTreatmentPlanItem;

public class ReopenTreatmentPlanItemCommandHandler(
    ILogger<ReopenTreatmentPlanItemCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache,
    IUser user)
    : IRequestHandler<ReopenTreatmentPlanItemCommand, Result<TreatmentPlanItemDto>>
{
    public async Task<Result<TreatmentPlanItemDto>> Handle(ReopenTreatmentPlanItemCommand request, CancellationToken cancellationToken)
    {
        var plan = await context.TreatmentPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken);
        if (plan is null)
        {
            logger.LogWarning("Plan item reopen failed. Plan {PlanId} not found.", request.PlanId);
            return ApplicationErrors.TreatmentPlanNotFound;
        }

        if (!user.CanWriteClinicalRecords() || !user.CanAccessAppointment(plan.DoctorId))
        {
            logger.LogWarning("Plan {PlanId} item reopen denied for user {UserId}.", request.PlanId, user.Id);
            return user.CanWriteClinicalRecords()
                ? ApplicationErrors.TreatmentPlanAccessForbidden
                : ApplicationErrors.ClinicalWriteForbidden;
        }

        var item = await context.TreatmentRecords
            .FirstOrDefaultAsync(r => r.Id == request.ItemId && r.PlanId == request.PlanId, cancellationToken);
        if (item is null)
        {
            logger.LogWarning("Plan {PlanId} item reopen failed. Item {ItemId} not found.", request.PlanId, request.ItemId);
            return ApplicationErrors.TreatmentPlanItemNotFound;
        }

        // D-P4: invoice lines are denormalized (no treatment id), so the guard
        // is conservative — an item linked to an appointment with an issued
        // invoice stays billed until the invoice is resolved. Issued invoices
        // are never rewritten retroactively.
        if (item.AppointmentId is { } appointmentId)
        {
            var onIssuedInvoice = await context.Invoices
                .AsNoTracking()
                .AnyAsync(i => i.AppointmentId == appointmentId && i.LifecycleStatus == InvoiceLifecycleStatus.Issued, cancellationToken);
            if (onIssuedInvoice)
            {
                logger.LogWarning(
                    "Plan item {ItemId} reopen blocked: its appointment {AppointmentId} already has an issued invoice.",
                    request.ItemId, appointmentId);
                return ApplicationErrors.TreatmentPlanItemOnIssuedInvoice;
            }
        }

        var result = item.Reopen();
        if (result.IsError)
        {
            logger.LogWarning("Failed to reopen plan item {ItemId}. Errors: {@Errors}", request.ItemId, result.Errors);
            return result.Errors;
        }

        await context.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync("treatment-plan", cancellationToken);
        await cache.RemoveByTagAsync("treatment-record", cancellationToken);

        return item.ToPlanItemDto();
    }
}