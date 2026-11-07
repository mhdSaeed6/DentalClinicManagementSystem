using System.ComponentModel.DataAnnotations;

namespace DentalClinic.Contracts.Requests.Staff;

public sealed class UpdateStaffUserRolesRequest
{
    [Required]
    public List<string> Roles { get; set; } = [];
}