namespace DentalClinic.Domain.Common.Interfaces;

public interface ISoftDeletable
{
    bool IsDeleted { get; }
    DateTimeOffset? DeletedAtUtc { get; }
    string? DeletedBy { get; }

    void Delete(string? deletedBy = null);
}