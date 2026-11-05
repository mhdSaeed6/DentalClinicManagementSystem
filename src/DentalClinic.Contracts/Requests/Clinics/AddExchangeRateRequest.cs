namespace DentalClinic.Contracts.Requests.Clinics;

/// <summary>
/// Appends a rate to the clinic's conversion history. When
/// <see cref="EffectiveFromUtc"/> is omitted the server stamps "now".
/// </summary>
public class AddExchangeRateRequest
{
    public string FromCurrency { get; set; } = null!;
    public string ToCurrency { get; set; } = null!;
    public decimal Rate { get; set; }
    public DateTimeOffset? EffectiveFromUtc { get; set; }
}