namespace DentalClinic.Domain.Common.Interfaces;

internal interface ISoftDeletable
{
    bool IsDeleted { get; }
    DateTimeOffset? DeletedAtUtc { get; }
    string? DeletedBy { get; }

    void Delete(string? deletedBy = null);
}