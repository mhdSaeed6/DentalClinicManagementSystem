using System.ComponentModel.DataAnnotations;

namespace DentalClinic.Contracts.Requests.Appointments;

public class CreateAppointmentRequest
{
    [Required(ErrorMessage = "Patient ID is required.")]
    public Guid PatientId { get; set; }

    [Required(ErrorMessage = "Doctor ID is required.")]
    public Guid DoctorId { get; set; }

    [Required(ErrorMessage = "Service ID is required.")]
    public Guid ServiceId { get; set; }

    [Required(ErrorMessage = "Scheduled date and time is required.")]
    public DateTime ScheduledDateTime { get; set; } = DateTime.UtcNow.AddDays(1);

    [Range(15, 240, ErrorMessage = "Duration must be between 15 and 240 minutes.")]
    public int DurationInMinutes { get; set; } = 30;

    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    public string? Notes { get; set; } = "Routine checkup";
}