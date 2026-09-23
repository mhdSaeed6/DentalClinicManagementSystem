using DentalClinic.Domain.Services;

namespace DentalClinic.Tests.Common.Builders;

public class DentalServiceTestDataBuilder : ITestDataBuilder<DentalService>
{
    private readonly string? _description = "Professional dental cleaning";
    private string _name = "Teeth Cleaning";
    private decimal _cost = 50m;

    public static DentalServiceTestDataBuilder Create() => new();

    public DentalServiceTestDataBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public DentalServiceTestDataBuilder WithCost(decimal cost)
    {
        _cost = cost;
        return this;
    }

    public DentalService Build()
    {
        return DentalService.Create(_name, _description, _cost).Value;
    }
}