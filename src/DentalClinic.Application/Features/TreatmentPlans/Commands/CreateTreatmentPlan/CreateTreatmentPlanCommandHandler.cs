using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.TreatmentPlans.Dtos;
using DentalClinic.Application.Features.TreatmentPlans.Mappers;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.TreatmentPlans;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.TreatmentPlans.Commands.CreateTreatmentPlan;

public class CreateTreatmentPlanCommandHandler(
    ILogger<CreateTreatmentPlanCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache,
    IUser user)
    : IRequestHandler<CreateTreatmentPlanCommand, Result<TreatmentPlanDto>>
{
    public async Task<Result<TreatmentPlanDto>> Handle(CreateTreatmentPlanCommand request, CancellationToken cancellationToken)
    {
        // Authorization: only admins and doctors author treatment plans, and a
        // doctor may only author plans for their own profile (two-layer rule,
        // enforced in the handler — never trust client-supplied doctor ids).
        if (!user.CanWriteClinicalRecords() || !user.CanAccessAppointment(request.DoctorId))
        {
            logger.LogWarning("Treatment plan creation denied for user {UserId} (doctor {DoctorId}).", user.Id, request.DoctorId);
            return user.CanWriteClinicalRecords()
                ? ApplicationErrors.TreatmentPlanAccessForbidden
                : ApplicationErrors.ClinicalWriteForbidden;
        }

        var patientName = await context.Patients
            .AsNoTracking()
            .Where(p => p.Id == request.PatientId)
            .Select(p => p.FirstName + " " + p.LastName)
            .FirstOrDefaultAsync(cancellationToken);
        if (patientName is null)
        {
            logger.LogWarning("Treatment plan creation failed. Patient {PatientId} not found.", request.PatientId);
            return ApplicationErrors.PatientNotFound;
        }

        var doctorName = await context.Doctors
            .AsNoTracking()
            .Where(d => d.Id == request.DoctorId)
            .Select(d => d.FirstName + " " + d.LastName)
            .FirstOrDefaultAsync(cancellationToken);
        if (doctorName is null)
        {
            logger.LogWarning("Treatment plan creation failed. Doctor {DoctorId} not found.", request.DoctorId);
            return ApplicationErrors.DoctorNotFound;
        }

        var result = TreatmentPlan.Create(
            request.PatientId,
            request.DoctorId,
            request.Title,
            request.Notes);

        if (result.IsError)
        {
            logger.LogWarning("Failed to create treatment plan. Errors: {@Errors}", result.Errors);
            return result.Errors;
        }

        context.TreatmentPlans.Add(result.Value);
        await context.SaveChangesAsync(cancellationToken);

        // Mutations invalidate both tags: plan items are treatment records.
        await cache.RemoveByTagAsync("treatment-plan", cancellationToken);
        await cache.RemoveByTagAsync("treatment-record", cancellationToken);

        logger.LogInformation("Treatment plan {PlanId} created for patient {PatientId}.", result.Value.Id, request.PatientId);

        return result.Value.ToDto(default(TreatmentPlanTotals), patientName, doctorName);
    }
}