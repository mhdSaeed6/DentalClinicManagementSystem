using System.ComponentModel.DataAnnotations;

namespace DentalClinic.Contracts.Requests.Staff;

public sealed class SetStaffUserActiveRequest
{
    /// <summary>A value of <c>false</c> permanently locks the account out.</summary>
    [Required]
    public bool IsActive { get; set; }
}