using System.ComponentModel.DataAnnotations;

namespace DentalClinic.Contracts.Requests.Patients;

public class MedicalHistoryRequest
{
    public bool HasDiabetes { get; set; }
    public bool HasHypertension { get; set; }
    public bool HasHeartDisease { get; set; }
    public bool HasBleedingDisorder { get; set; }
    public bool HasEpilepsy { get; set; }
    public bool HasAsthma { get; set; }
    public bool IsPregnant { get; set; }

    [Range(1, 45, ErrorMessage = "Pregnancy weeks must be between 1 and 45.")]
    public int? PregnancyWeeks { get; set; }

    [StringLength(20, ErrorMessage = "Blood pressure reading must not exceed 20 characters.")]
    public string? BloodPressureReading { get; set; }
    public string? Allergies { get; set; }
    public string? Notes { get; set; }
}