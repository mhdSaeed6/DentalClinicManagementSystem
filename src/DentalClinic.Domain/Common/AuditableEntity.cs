using DentalClinic.Domain.Common.Interfaces;

namespace DentalClinic.Domain.Common.Results;

public abstract class AuditableEntity : Entity, ISoftDeletable
{
    protected AuditableEntity()
    { }

    protected AuditableEntity(Guid id)
    : base(id)
    {
    }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    public void Delete(string? deletedBy = null)
    {
        IsDeleted = true;
        DeletedAtUtc = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy?.Trim();
    }
}