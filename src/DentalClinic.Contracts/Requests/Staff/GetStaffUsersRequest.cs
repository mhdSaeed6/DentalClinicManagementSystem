using System.ComponentModel.DataAnnotations;

namespace DentalClinic.Contracts.Requests.Staff;

public sealed class GetStaffUsersRequest
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;

    /// <summary>Case-insensitive filter on email or user name.</summary>
    public string? SearchTerm { get; set; }
}