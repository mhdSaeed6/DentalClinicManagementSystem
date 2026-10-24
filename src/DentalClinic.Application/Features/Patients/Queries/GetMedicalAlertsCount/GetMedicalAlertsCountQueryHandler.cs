using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace DentalClinic.Application.Features.Patients.Queries.GetMedicalAlertsCount;

public class GetMedicalAlertsCountQueryHandler(IAppDbContext context)
    : IRequestHandler<GetMedicalAlertsCountQuery, Result<int>>
{
    public async Task<Result<int>> Handle(GetMedicalAlertsCountQuery request, CancellationToken cancellationToken)
    {
        var query = context.Patients.AsNoTracking();

        // Same trusted, server-derived scope as the patient registry, applied
        // before any filter so a restricted doctor only counts their patients.
        if (request.Scope.IsRestricted)
        {
            var scopeDoctorId = request.Scope.DoctorId ?? Guid.Empty;
            var scopeUserId = request.Scope.UserId;

            if (!string.IsNullOrEmpty(scopeUserId))
            {
                var userId = scopeUserId;
                query = query.Where(p =>
                    p.CreatedBy == userId ||
                    context.Appointments.Any(a => a.PatientId == p.Id && a.DoctorId == scopeDoctorId));
            }
            else
            {
                query = query.Where(p =>
                    context.Appointments.Any(a => a.PatientId == p.Id && a.DoctorId == scopeDoctorId));
            }
        }

        // Deliberately coarser than MedicalAlertEvaluator: anything the
        // evaluator reports (condition flags, a non-empty allergy list or a
        // recorded blood pressure) counts as a patient with an alert here.
        // Parsing "systolic/diastolic" strings is not attempted in SQL; the
        // domain evaluator owns the severity rules, this only needs a count.
        query = query.Where(p =>
            p.MedicalHistory.HasDiabetes ||
            p.MedicalHistory.HasHypertension ||
            p.MedicalHistory.HasHeartDisease ||
            p.MedicalHistory.HasBleedingDisorder ||
            p.MedicalHistory.HasEpilepsy ||
            p.MedicalHistory.HasAsthma ||
            p.MedicalHistory.IsPregnant ||
            p.MedicalHistory.BloodPressureReading != null ||
            p.MedicalHistory.Allergies != null);

        return await query.CountAsync(cancellationToken);
    }
}
