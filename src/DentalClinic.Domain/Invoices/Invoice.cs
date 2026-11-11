using DentalClinic.Domain.Common.Interfaces;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Invoices.Enums;

namespace DentalClinic.Domain.Invoices;

public class Invoice : AuditableEntity, IRequireAuditTrail
{
    /// <summary>Upper bound for <see cref="InvoiceNumber"/>, mirrored by the EF configuration.</summary>
    public const int MaxInvoiceNumberLength = 32;

    private readonly List<Payment> _payments = [];
    private readonly List<InvoiceLine> _lines = [];

    public Guid PatientId { get; private set; }
    public Guid? AppointmentId { get; private set; }

    /// <summary>
    /// Human-readable identity of the bill, e.g. <c>INV-2026-0042</c>. Assigned
    /// exactly once, on the Draft → Issued transition, and never mutated
    /// afterwards. Drafts are internal working documents and stay unnumbered
    /// until they become a real bill.
    /// </summary>
    public string? InvoiceNumber { get; private set; }

    /// <summary>
    /// When the invoice became a bill. Null while it is still a draft.
    /// </summary>
    public DateTimeOffset? IssuedAtUtc { get; private set; }

    public decimal TotalAmount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal RemainingAmount => TotalAmount - PaidAmount;
    public PaymentStatus Status { get; private set; }
    public InvoiceLifecycleStatus LifecycleStatus { get; private set; }

    /// <summary>
    /// SQL Server <c>rowversion</c> token used for optimistic concurrency.
    /// EF appends it to every UPDATE/DELETE predicate, so a concurrent writer
    /// that loaded a stale copy cannot silently overwrite this invoice.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<Payment> Payments => _payments.AsReadOnly();
    public IReadOnlyCollection<InvoiceLine> Lines => _lines.AsReadOnly();

    private Invoice() { }

    private Invoice(
        Guid id,
        Guid patientId,
        Guid? appointmentId,
        decimal totalAmount,
        InvoiceLifecycleStatus lifecycleStatus,
        string? invoiceNumber,
        DateTimeOffset? issuedAtUtc)
        : base(id)
    {
        PatientId = patientId;
        AppointmentId = appointmentId;
        TotalAmount = totalAmount;
        PaidAmount = 0;
        Status = PaymentStatus.Unpaid;
        LifecycleStatus = lifecycleStatus;
        InvoiceNumber = invoiceNumber;
        IssuedAtUtc = issuedAtUtc;
    }

    /// <summary>
    /// Creates an invoice. Standalone/manual invoices default to
    /// <see cref="InvoiceLifecycleStatus.Issued"/> so they remain immediately
    /// payable exactly as before the lifecycle existed; only
    /// appointment-derived drafts pass <see cref="InvoiceLifecycleStatus.Draft"/>.
    /// </summary>
    /// <param name="invoiceNumber">
    /// The allocated number when the invoice is born issued. It must be supplied
    /// by the caller (the numbering service), never derived here: the domain has
    /// no clock, no counter and no knowledge of the clinic's year.
    /// </param>
    /// <param name="issuedAtUtc">The issuance instant, supplied by the caller.</param>
    public static Result<Invoice> Create(
        Guid patientId,
        decimal totalAmount,
        Guid? appointmentId = null,
        InvoiceLifecycleStatus lifecycleStatus = InvoiceLifecycleStatus.Issued,
        string? invoiceNumber = null,
        DateTimeOffset? issuedAtUtc = null)
    {
        if (totalAmount <= 0)
        {
            return InvoiceErrors.InvalidTotalAmount;
        }

        // A draft is not a bill yet, so it must never carry a number: doing so
        // would let two drafts hold the same number or leak a number that was
        // later abandoned.
        if (lifecycleStatus == InvoiceLifecycleStatus.Draft && invoiceNumber is not null)
        {
            return InvoiceErrors.InvalidState;
        }

        var normalizedNumber = invoiceNumber;
        if (invoiceNumber is not null)
        {
            var normalization = NormalizeInvoiceNumber(invoiceNumber);
            if (normalization.IsError)
            {
                return normalization.Errors;
            }

            normalizedNumber = normalization.Value;
        }

        return new Invoice(
            Guid.NewGuid(),
            patientId,
            appointmentId,
            totalAmount,
            lifecycleStatus,
            normalizedNumber,
            issuedAtUtc);
    }

    public Result<Updated> AddLine(string description, decimal amount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        if (amount <= 0)
        {
            return InvoiceErrors.InvalidLineAmount;
        }

        if (IsIssuedOrLocked())
        {
            return InvoiceErrors.CannotModifyIssuedInvoice;
        }

        var line = InvoiceLine.Create(description.Trim(), amount);
        line.SetInvoice(Id);
        _lines.Add(line);
        TotalAmount = _lines.Sum(l => l.Amount);
        PaidAmount = 0;
        Status = PaymentStatus.Unpaid;
        HasManualAdjustments = true;
        return Result.Updated;
    }

    public void AddLineImmediate(string description, decimal amount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        if (amount <= 0) return;

        var line = InvoiceLine.Create(description.Trim(), amount);
        line.SetInvoice(Id);
        _lines.Add(line);
    }

    public void RecalculateFromLines()
    {
        TotalAmount = _lines.Sum(l => l.Amount);
        if (TotalAmount < 0) TotalAmount = 0;
        PaidAmount = 0;
        Status = PaymentStatus.Unpaid;
    }

    public Result<Updated> RemoveLine(Guid lineId)
    {
        if (IsIssuedOrLocked())
        {
            return InvoiceErrors.CannotModifyIssuedInvoice;
        }

        var line = _lines.FirstOrDefault(l => l.Id == lineId);
        if (line is null)
        {
            return InvoiceErrors.InvoiceLineNotFound;
        }

        _lines.Remove(line);
        TotalAmount = _lines.Sum(l => l.Amount);
        PaidAmount = 0;
        Status = PaymentStatus.Unpaid;
        HasManualAdjustments = true;
        return Result.Updated;
    }

    /// <summary>
    /// True while the draft's contents still match what was generated from the
    /// source appointment. Any manual edit clears this so a later refresh can
    /// detect (and refuse to silently discard) manual work.
    /// </summary>
    public bool HasManualAdjustments { get; private set; }

    /// <summary>
    /// Guards that the invoice may still be changed. Issued invoices, and any
    /// invoice that has recorded a payment, are locked.
    /// </summary>
    public Result<Updated> EnsureEditable()
    {
        return IsIssuedOrLocked()
            ? InvoiceErrors.CannotModifyIssuedInvoice
            : Result.Updated;
    }

    /// <summary>
    /// Applies a desired set of lines and recomputes the total on the server.
    /// A change carrying a known <see cref="Guid"/> updates that line in place so
    /// its identifier stays stable; a change without one creates a new line.
    /// Existing lines that are not mentioned are removed.
    /// Returns the created and removed lines so the caller can persist them.
    /// </summary>
    public Result<(IReadOnlyList<InvoiceLine> Added, IReadOnlyList<InvoiceLine> Removed)> ApplyLineChanges(
        IReadOnlyList<(Guid? LineId, string Description, decimal Amount)> changes)
    {
        ArgumentNullException.ThrowIfNull(changes);

        var editable = EnsureEditable();
        if (editable.IsError)
        {
            return editable.Errors;
        }

        var priced = changes
            .Where(c => !string.IsNullOrWhiteSpace(c.Description) && c.Amount > 0)
            .ToList();

        if (priced.Count == 0)
        {
            return InvoiceErrors.NoBillableItems;
        }

        var existing = _lines.ToDictionary(l => l.Id);
        var kept = new HashSet<Guid>();
        var added = new List<InvoiceLine>();

        foreach (var change in priced)
        {
            if (change.LineId is { } id && existing.TryGetValue(id, out var line))
            {
                line.Update(change.Description.Trim(), change.Amount);
                kept.Add(line.Id);
                continue;
            }

            var created = InvoiceLine.Create(change.Description.Trim(), change.Amount);
            created.SetInvoice(Id);
            _lines.Add(created);
            kept.Add(created.Id);
            added.Add(created);
        }

        var removed = new List<InvoiceLine>();
        foreach (var line in _lines.ToList())
        {
            if (!kept.Contains(line.Id))
            {
                _lines.Remove(line);
                removed.Add(line);
            }
        }

        TotalAmount = _lines.Sum(l => l.Amount);
        PaidAmount = 0;
        Status = PaymentStatus.Unpaid;
        HasManualAdjustments = true;

        return (added, removed);
    }

    public Result<Updated> UpdateLine(Guid lineId, string description, decimal amount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        if (amount <= 0)
        {
            return InvoiceErrors.InvalidLineAmount;
        }

        if (IsIssuedOrLocked())
        {
            return InvoiceErrors.CannotModifyIssuedInvoice;
        }

        var line = _lines.FirstOrDefault(l => l.Id == lineId);
        if (line is null)
        {
            return InvoiceErrors.InvoiceLineNotFound;
        }

        line.Update(description.Trim(), amount);
        TotalAmount = _lines.Sum(l => l.Amount);
        PaidAmount = 0;
        Status = PaymentStatus.Unpaid;
        HasManualAdjustments = true;
        return Result.Updated;
    }

    /// <summary>
    /// Turns a draft into a bill. The number is allocated by the caller (see
    /// <c>IInvoiceNumberGenerator</c>) and written here exactly once: re-issuing
    /// an already-issued invoice succeeds without touching it, so a repeated
    /// request is safe and keeps the original number.
    /// </summary>
    public Result<Updated> Issue(string invoiceNumber, DateTimeOffset issuedAtUtc)
    {
        if (LifecycleStatus == InvoiceLifecycleStatus.Issued)
        {
            return Result.Updated;
        }

        var normalization = NormalizeInvoiceNumber(invoiceNumber);
        if (normalization.IsError)
        {
            return normalization.Errors;
        }

        if (PaidAmount > 0)
        {
            return InvoiceErrors.InvalidState;
        }

        if (_lines.Count == 0)
        {
            return InvoiceErrors.NoBillableItems;
        }

        // Validate the contents and derive the total from the persisted lines
        // rather than trusting the stored total.
        RecalculateFromLines();

        if (TotalAmount <= 0)
        {
            return InvoiceErrors.InvalidTotalAmount;
        }

        // Issuance never registers a payment; PaymentStatus stays Unpaid.
        LifecycleStatus = InvoiceLifecycleStatus.Issued;
        InvoiceNumber = normalization.Value;
        IssuedAtUtc = issuedAtUtc;
        return Result.Updated;
    }

    public Result<Updated> RegisterPayment(decimal amount)
    {
        if (LifecycleStatus == InvoiceLifecycleStatus.Draft)
        {
            return InvoiceErrors.CannotPayDraft;
        }

        if (amount <= 0)
        {
            return InvoiceErrors.InvalidPaymentAmount;
        }

        if (Status == PaymentStatus.Paid)
        {
            return InvoiceErrors.AlreadyFullyPaid;
        }

        if (amount > RemainingAmount)
        {
            return InvoiceErrors.PaymentExceedsRemainingAmount;
        }

        PaidAmount += amount;
        Status = (PaidAmount == TotalAmount) ? PaymentStatus.Paid : PaymentStatus.PartiallyPaid;

        return Result.Updated;
    }

    private bool IsIssuedOrLocked()
    {
        return LifecycleStatus == InvoiceLifecycleStatus.Issued || Status != PaymentStatus.Unpaid;
    }

    /// <summary>
    /// Trims and length-checks a number. Uniqueness is enforced by the database
    /// (a filtered unique index), not here: the domain cannot see other invoices.
    /// </summary>
    private static Result<string> NormalizeInvoiceNumber(string invoiceNumber)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
        {
            return InvoiceErrors.InvalidInvoiceNumber;
        }

        var trimmed = invoiceNumber.Trim();
        return trimmed.Length > MaxInvoiceNumberLength
            ? InvoiceErrors.InvalidInvoiceNumber
            : trimmed;
    }
}