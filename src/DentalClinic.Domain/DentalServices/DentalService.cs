using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Domain.Services;

public class DentalService : AuditableEntity
{
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public bool IsActive { get; private set; }

    private DentalService() { }

    private DentalService(
        Guid id,
        string name,
        string? description,
        decimal price)
        : base(id)
    {
        Name = name;
        Description = description;
        Price = price;
        IsActive = true;
    }

    public static Result<DentalService> Create(
        string name,
        string? description,
        decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return ServiceErrors.NameRequired;
        }

        if (price < 0)
        {
            return ServiceErrors.InvalidPrice;
        }

        return new DentalService(
            Guid.NewGuid(),
            name.Trim(),
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            price);
    }

    public Result<Updated> Update(
        string name,
        string? description,
        decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return ServiceErrors.NameRequired;
        }

        if (price < 0)
        {
            return ServiceErrors.InvalidPrice;
        }

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Price = price;

        return Result.Updated;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}