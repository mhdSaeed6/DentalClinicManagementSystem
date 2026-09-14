namespace DentalClinic.Contracts.Requests.Patients;

public class MedicalHistoryRequest
{
    public bool HasDiabetes { get; set; }
    public bool HasHypertension { get; set; }
    public bool HasHeartDisease { get; set; }
    public string? Allergies { get; set; }
    public string? Notes { get; set; }
}