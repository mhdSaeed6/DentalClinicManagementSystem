using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Domain.Clinics;

/// <summary>
/// One row of the clinic's currency-conversion history: how many units of
/// <see cref="ToCurrency"/> one unit of <see cref="FromCurrency"/> was worth
/// from <see cref="EffectiveFromUtc"/> onwards.
/// </summary>
/// <remarks>
/// <para>
/// Append-only by design. A rate is never edited or deleted — an old printout
/// must stay explainable — so a correction is a new row with a later effective
/// date, and <see cref="PickLatest"/> makes the newest one win.
/// </para>
/// <para>
/// Money is stored in the clinic's base currency; the secondary currency is a
/// presentation concern, which is why no invoice or payment points here. That
/// keeps a rate change from silently rewriting historical bills.
/// </para>
/// </remarks>
public sealed class ExchangeRate : AuditableEntity
{
    public string FromCurrency { get; private set; } = default!;
    public string ToCurrency { get; private set; } = default!;

    /// <summary>Units of <see cref="ToCurrency"/> per one unit of <see cref="FromCurrency"/>.</summary>
    public decimal Rate { get; private set; }

    /// <summary>Instant from which this rate applies. Back-dating is allowed on purpose.</summary>
    public DateTimeOffset EffectiveFromUtc { get; private set; }

    private ExchangeRate()
    {
    }

    private ExchangeRate(
        Guid id,
        string fromCurrency,
        string toCurrency,
        decimal rate,
        DateTimeOffset effectiveFromUtc)
        : base(id)
    {
        FromCurrency = fromCurrency;
        ToCurrency = toCurrency;
        Rate = rate;
        EffectiveFromUtc = effectiveFromUtc;
    }

    public static Result<ExchangeRate> Create(
        string fromCurrency,
        string toCurrency,
        decimal rate,
        DateTimeOffset effectiveFromUtc)
    {
        if (!IsCurrencyCode(fromCurrency) || !IsCurrencyCode(toCurrency))
        {
            return ExchangeRateErrors.InvalidCurrency;
        }

        var from = fromCurrency.Trim().ToUpperInvariant();
        var to = toCurrency.Trim().ToUpperInvariant();

        if (string.Equals(from, to, StringComparison.Ordinal))
        {
            return ExchangeRateErrors.SameCurrency;
        }

        if (rate <= 0)
        {
            return ExchangeRateErrors.InvalidRate;
        }

        if (effectiveFromUtc == default)
        {
            return ExchangeRateErrors.InvalidEffectiveDate;
        }

        return new ExchangeRate(Guid.NewGuid(), from, to, rate, effectiveFromUtc);
    }

    /// <summary>
    /// The rate in force at <paramref name="asOfUtc"/>: the newest row whose
    /// effective instant has already passed, or <see langword="null"/> when the
    /// history does not reach back that far.
    /// </summary>
    /// <remarks>
    /// Ties resolve to the most recently created row, so a same-instant
    /// correction always supersedes what it corrects.
    /// </remarks>
    public static ExchangeRate? PickLatest(IEnumerable<ExchangeRate> rates, DateTimeOffset asOfUtc)
    {
        ArgumentNullException.ThrowIfNull(rates);

        return rates
            .Where(rate => rate.EffectiveFromUtc <= asOfUtc)
            .OrderByDescending(rate => rate.EffectiveFromUtc)
            .ThenByDescending(rate => rate.CreatedAtUtc)
            .FirstOrDefault();
    }

    private static bool IsCurrencyCode(string? value)
        => !string.IsNullOrWhiteSpace(value)
           && value.Trim().Length == 3
           && value.Trim().All(char.IsLetter);
}