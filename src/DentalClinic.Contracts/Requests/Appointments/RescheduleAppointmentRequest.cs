using System.ComponentModel.DataAnnotations;

namespace DentalClinic.Contracts.Requests.Appointments;

public class RescheduleAppointmentRequest
{
    [Required(ErrorMessage = "New scheduled date and time is required.")]
    public DateTime NewScheduledDateTime { get; set; }

    [Range(15, 240, ErrorMessage = "Duration must be between 15 and 240 minutes.")]
    public int DurationInMinutes { get; set; }
}