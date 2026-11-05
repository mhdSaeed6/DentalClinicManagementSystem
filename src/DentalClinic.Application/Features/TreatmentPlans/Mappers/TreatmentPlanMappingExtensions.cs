using DentalClinic.Application.Features.TreatmentPlans.Dtos;
using DentalClinic.Domain.TreatmentPlans;
using DentalClinic.Domain.TreatmentRecords;

namespace DentalClinic.Application.Features.TreatmentPlans.Mappers;

public static class TreatmentPlanMappingExtensions
{
    public static TreatmentPlanItemDto ToPlanItemDto(this TreatmentRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new TreatmentPlanItemDto(
            record.Id,
            record.PlanId,
            record.PatientId,
            record.DoctorId,
            record.AppointmentId,
            record.ToothNumber,
            record.ProcedureDetails,
            record.Status,
            record.EstimatedCost,
            record.Cost,
            record.SessionNumber);
    }

    public static List<TreatmentPlanItemDto> ToPlanItemDtos(this IEnumerable<TreatmentRecord> records)
    {
        return [.. records.Select(r => r.ToPlanItemDto())];
    }

    // Totals: planned items contribute their estimate, performed items their
    // charged cost, cancelled items contribute nothing (they remain in history
    // but are never billable).
    public static TreatmentPlanTotals ComputeTotals(this IReadOnlyCollection<TreatmentRecord> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        decimal estimated = 0;
        decimal performed = 0;

        foreach (var item in items)
        {
            switch (item.Status)
            {
                case TreatmentRecordStatus.Planned:
                    estimated += item.EstimatedCost ?? 0;
                    break;
                case TreatmentRecordStatus.Performed:
                    performed += item.Cost;
                    break;
            }
        }

        return new TreatmentPlanTotals(items.Count, estimated, performed);
    }

    public static TreatmentPlanDto ToDto(
        this TreatmentPlan plan,
        TreatmentPlanTotals totals,
        string patientName,
        string doctorName)
    {
        ArgumentNullException.ThrowIfNull(plan);

        return new TreatmentPlanDto(
            plan.Id,
            plan.PatientId,
            plan.DoctorId,
            patientName,
            doctorName,
            plan.Title,
            plan.Notes,
            plan.Status,
            plan.CreatedAtUtc,
            totals.ItemCount,
            totals.EstimatedTotal,
            totals.PerformedTotal);
    }

    public static TreatmentPlanDetailDto ToDetailDto(
        this TreatmentPlan plan,
        IReadOnlyList<TreatmentRecord> items,
        string patientName,
        string doctorName)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var totals = items.ComputeTotals();

        return new TreatmentPlanDetailDto(
            plan.Id,
            plan.PatientId,
            plan.DoctorId,
            patientName,
            doctorName,
            plan.Title,
            plan.Notes,
            plan.Status,
            plan.CreatedAtUtc,
            totals.ItemCount,
            totals.EstimatedTotal,
            totals.PerformedTotal,
            items.ToPlanItemDtos());
    }
}