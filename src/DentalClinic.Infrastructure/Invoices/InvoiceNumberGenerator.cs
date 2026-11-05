using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Domain.Invoices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DentalClinic.Infrastructure.Invoices;

/// <summary>
/// Allocates invoice numbers from a per-year counter row.
/// </summary>
/// <remarks>
/// <para>
/// The counter is read with <c>UPDLOCK, HOLDLOCK</c> so the read takes an
/// exclusive lock that is held until the caller's transaction ends. A second
/// issuer blocks there instead of reading the same value, which is what makes
/// "read, increment, write" safe without a retry loop or a deadlock.
/// </para>
/// <para>
/// The increment is persisted by the caller's <c>SaveChangesAsync</c> in the
/// same transaction as the invoice, so a rolled-back issuance also releases the
/// number. Gaps can still occur if the process dies between commit and response
/// — acceptable, and far better than a duplicate.
/// </para>
/// </remarks>
public sealed class InvoiceNumberGenerator(
    IAppDbContext context,
    TimeProvider timeProvider,
    IOptionsMonitor<InvoiceNumberingSettings> options,
    ILogger<InvoiceNumberGenerator> logger) : IInvoiceNumberGenerator
{
    private readonly IAppDbContext _context = context;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly IOptionsMonitor<InvoiceNumberingSettings> _options = options;
    private readonly ILogger<InvoiceNumberGenerator> _logger = logger;

    private string? _cachedTimeZoneId;
    private TimeZoneInfo? _cachedTimeZone;

    public async Task<string> NextAsync(CancellationToken cancellationToken)
    {
        if (!_context.HasActiveTransaction)
        {
            throw new InvalidOperationException(
                "Invoice numbers must be allocated inside the transaction that persists the invoice. " +
                "Open one with IAppDbContext.BeginTransactionAsync before calling NextAsync.");
        }

        var settings = _options.CurrentValue;
        var year = await CurrentClinicYearAsync(settings, cancellationToken);

        // Raw SQL is the only way to express the lock hint through EF. The row
        // is materialised directly (no LINQ composition) so the hint stays on
        // the real table rather than inside a derived table.
        var counter = (await _context.InvoiceNumberCounters
                .FromSqlInterpolated($"SELECT * FROM [InvoiceNumberCounters] WITH (UPDLOCK, HOLDLOCK) WHERE [Year] = {year}")
                .ToListAsync(cancellationToken))
            .FirstOrDefault();

        if (counter is null)
        {
            counter = new InvoiceNumberCounter(year, 0);
            _context.InvoiceNumberCounters.Add(counter);
        }

        var sequence = counter.Increment();
        var invoiceNumber = InvoiceNumber.Format(settings.Prefix, settings.Padding, year, sequence);

        _logger.LogInformation(
            "Reserved invoice number {InvoiceNumber} for year {Year} (sequence {Sequence}).",
            invoiceNumber,
            year,
            sequence);

        return invoiceNumber;
    }

    /// <summary>
    /// The year of the number must be the clinic's calendar year, not UTC's:
    /// an invoice issued at 23:30 on 31 December in Cairo belongs to that year,
    /// even though UTC has already rolled over.
    /// </summary>
    /// <remarks>
    /// The clinic profile is the source of truth for the timezone; the
    /// <c>InvoiceNumbering</c> section survives only as a deployment-level
    /// fallback for the window before the settings row exists.
    /// </remarks>
    private async Task<int> CurrentClinicYearAsync(
        InvoiceNumberingSettings settings,
        CancellationToken cancellationToken)
    {
        var clinicTimeZoneId = await _context.ClinicSettings
            .AsNoTracking()
            .Select(c => c.TimeZoneId)
            .FirstOrDefaultAsync(cancellationToken);

        var timeZoneId = string.IsNullOrWhiteSpace(clinicTimeZoneId)
            ? settings.TimeZoneId
            : clinicTimeZoneId;

        var timeZone = ResolveTimeZone(timeZoneId);

        return TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow(), timeZone).Year;
    }

    private TimeZoneInfo ResolveTimeZone(string? configuredId)
    {
        if (_cachedTimeZone is not null && string.Equals(_cachedTimeZoneId, configuredId, StringComparison.Ordinal))
        {
            return _cachedTimeZone;
        }

        TimeZoneInfo resolved;
        if (string.IsNullOrWhiteSpace(configuredId))
        {
            resolved = TimeZoneInfo.Local;
        }
        else
        {
            try
            {
                resolved = TimeZoneInfo.FindSystemTimeZoneById(configuredId);
            }
            catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
            {
                // A typo must not stop billing; fall back to the host timezone
                // (set by TZ in the container) and say so loudly.
                _logger.LogWarning(
                    ex,
                    "Clinic timezone '{TimeZoneId}' is not a known timezone. Falling back to the host timezone {HostTimeZone}.",
                    configuredId,
                    TimeZoneInfo.Local.Id);

                resolved = TimeZoneInfo.Local;
            }
        }

        _cachedTimeZoneId = configuredId;
        _cachedTimeZone = resolved;
        return resolved;
    }
}