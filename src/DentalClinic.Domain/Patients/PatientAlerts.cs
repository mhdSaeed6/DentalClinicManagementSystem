using System.Linq.Expressions;

namespace DentalClinic.Domain.Patients;

/// <summary>
/// The single definition of "this patient carries a medical alert", written as
/// an expression tree so EF can translate it into SQL and every screen counts
/// the same patients.
/// </summary>
/// <remarks>
/// Deliberately coarser than <see cref="MedicalAlertEvaluator"/>: anything the
/// evaluator reports (a condition flag, a non-empty allergy list or a recorded
/// blood pressure) counts here. Parsing "systolic/diastolic" strings is not
/// attempted in SQL; the evaluator owns the severity rules, this only answers
/// "does this patient have one?".
/// </remarks>
public static class PatientAlerts
{
    public static readonly Expression<Func<Patient, bool>> Predicate = patient =>
        patient.MedicalHistory.HasDiabetes ||
        patient.MedicalHistory.HasHypertension ||
        patient.MedicalHistory.HasHeartDisease ||
        patient.MedicalHistory.HasBleedingDisorder ||
        patient.MedicalHistory.HasEpilepsy ||
        patient.MedicalHistory.HasAsthma ||
        patient.MedicalHistory.IsPregnant ||
        patient.MedicalHistory.BloodPressureReading != null ||
        patient.MedicalHistory.Allergies != null;
}