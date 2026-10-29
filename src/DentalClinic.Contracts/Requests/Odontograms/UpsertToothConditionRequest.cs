using System.ComponentModel.DataAnnotations;

namespace DentalClinic.Contracts.Requests.Odontograms;

public class UpsertToothConditionRequest
{
    [Range(0, 7, ErrorMessage = "Condition must be a defined tooth condition (0-7).")]
    public int Condition { get; set; }

    [Range(0, 5, ErrorMessage = "Surface must be a defined tooth surface or omitted (0-5).")]
    public int? Surface { get; set; }

    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    public string? Notes { get; set; }
}