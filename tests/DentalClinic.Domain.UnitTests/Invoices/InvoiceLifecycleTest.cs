using DentalClinic.Domain.Invoices;
using DentalClinic.Domain.Invoices.Enums;

namespace DentalClinic.Domain.UnitTests.Invoices;

public class InvoiceLifecycleTest
{
    private const string TestNumber = "INV-2026-0001";

    private static readonly DateTimeOffset TestIssuedAt =
        new(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);

    private static Invoice DraftInvoice(params (string Description, decimal Amount)[] lines)
    {
        var invoice = Invoice.Create(
            Guid.NewGuid(),
            lines.Sum(l => l.Amount),
            Guid.NewGuid(),
            InvoiceLifecycleStatus.Draft).Value;

        foreach (var line in lines)
        {
            invoice.AddLineImmediate(line.Description, line.Amount);
        }

        return invoice;
    }

    // -----------------------------------------------------------------------
    // Legacy compatibility
    // -----------------------------------------------------------------------

    [Fact]
    public void CreateInvoice_WithoutExplicitLifecycle_ShouldBeIssuedAndImmediatelyPayable()
    {
        // Standalone/manual invoices were payable before the lifecycle existed,
        // so they must not silently become unpayable drafts.
        var invoice = Invoice.Create(Guid.NewGuid(), 100m).Value;

        Assert.Equal(InvoiceLifecycleStatus.Issued, invoice.LifecycleStatus);
        Assert.True(invoice.RegisterPayment(25m).IsSuccess);
        Assert.Equal(PaymentStatus.PartiallyPaid, invoice.Status);
    }

    [Fact]
    public void CreateInvoice_WithExplicitDraft_ShouldStartUnpaidAndUnissued()
    {
        var invoice = Invoice.Create(Guid.NewGuid(), 100m, null, InvoiceLifecycleStatus.Draft).Value;

        Assert.Equal(InvoiceLifecycleStatus.Draft, invoice.LifecycleStatus);
        Assert.Equal(PaymentStatus.Unpaid, invoice.Status);
        Assert.Equal(0m, invoice.PaidAmount);
    }

    [Fact]
    public void CreateInvoice_BornIssued_ShouldCarryTheSuppliedNumber()
    {
        // A standalone invoice is a bill the moment it is created, so it takes
        // its number from the caller instead of waiting for an Issue() call.
        var invoice = Invoice.Create(
            Guid.NewGuid(),
            100m,
            null,
            InvoiceLifecycleStatus.Issued,
            TestNumber,
            TestIssuedAt).Value;

        Assert.Equal(InvoiceLifecycleStatus.Issued, invoice.LifecycleStatus);
        Assert.Equal(TestNumber, invoice.InvoiceNumber);
        Assert.Equal(TestIssuedAt, invoice.IssuedAtUtc);
    }

    [Fact]
    public void CreateInvoice_DraftCarryingANumber_ShouldBeRejected()
    {
        // A draft is not a bill; numbering it would let two drafts share a
        // number or leak one that is later abandoned.
        var result = Invoice.Create(
            Guid.NewGuid(),
            100m,
            null,
            InvoiceLifecycleStatus.Draft,
            TestNumber,
            TestIssuedAt);

        Assert.True(result.IsError);
        Assert.Equal(InvoiceErrors.InvalidState.Code, result.TopError.Code);
    }

    // -----------------------------------------------------------------------
    // Draft cannot be paid
    // -----------------------------------------------------------------------

    [Fact]
    public void RegisterPayment_OnDraft_ShouldBeRejected()
    {
        var invoice = DraftInvoice(("Cleaning", 80m));

        var result = invoice.RegisterPayment(10m);

        Assert.True(result.IsError);
        Assert.Equal(InvoiceErrors.CannotPayDraft.Code, result.TopError.Code);
        Assert.Equal(0m, invoice.PaidAmount);
        Assert.Equal(PaymentStatus.Unpaid, invoice.Status);
    }

    // -----------------------------------------------------------------------
    // Issuance
    // -----------------------------------------------------------------------

    [Fact]
    public void Issue_OnDraft_ShouldBecomeIssuedAndNotRegisterPayment()
    {
        var invoice = DraftInvoice(("Cleaning", 80m), ("Filling", 45m));

        var result = invoice.Issue(TestNumber, TestIssuedAt);

        Assert.True(result.IsSuccess);
        Assert.Equal(InvoiceLifecycleStatus.Issued, invoice.LifecycleStatus);
        Assert.Equal(0m, invoice.PaidAmount);
        Assert.Equal(PaymentStatus.Unpaid, invoice.Status);
        Assert.Equal(125m, invoice.TotalAmount);
    }

    [Fact]
    public void Issue_WhenAlreadyIssued_ShouldBeIdempotentAndNotChangeTotals()
    {
        var invoice = DraftInvoice(("Cleaning", 80m));
        invoice.Issue(TestNumber, TestIssuedAt);
        invoice.RegisterPayment(30m);

        // A second issuance must not consume the caller's new number.
        var second = invoice.Issue("INV-2026-9999", TestIssuedAt.AddDays(1));

        Assert.True(second.IsSuccess);
        Assert.Equal(InvoiceLifecycleStatus.Issued, invoice.LifecycleStatus);
        Assert.Equal(TestNumber, invoice.InvoiceNumber);
        Assert.Equal(TestIssuedAt, invoice.IssuedAtUtc);
        Assert.Equal(30m, invoice.PaidAmount);
        Assert.Equal(PaymentStatus.PartiallyPaid, invoice.Status);
        Assert.Equal(80m, invoice.TotalAmount);
    }

    [Fact]
    public void Issue_OnDraft_ShouldAssignTheNumberAndIssuanceInstantExactlyOnce()
    {
        var invoice = DraftInvoice(("Cleaning", 80m));

        Assert.Null(invoice.InvoiceNumber);
        Assert.Null(invoice.IssuedAtUtc);

        var result = invoice.Issue(TestNumber, TestIssuedAt);

        Assert.True(result.IsSuccess);
        Assert.Equal(TestNumber, invoice.InvoiceNumber);
        Assert.Equal(TestIssuedAt, invoice.IssuedAtUtc);
    }

    [Fact]
    public void Issue_WithBlankNumber_ShouldBeRejectedAndStayDraft()
    {
        var invoice = DraftInvoice(("Cleaning", 80m));

        var result = invoice.Issue("   ", TestIssuedAt);

        Assert.True(result.IsError);
        Assert.Equal(InvoiceErrors.InvalidInvoiceNumber.Code, result.TopError.Code);
        Assert.Equal(InvoiceLifecycleStatus.Draft, invoice.LifecycleStatus);
        Assert.Null(invoice.InvoiceNumber);
    }

    [Fact]
    public void Issue_WithOverlongNumber_ShouldBeRejected()
    {
        var invoice = DraftInvoice(("Cleaning", 80m));

        var result = invoice.Issue(new string('X', Invoice.MaxInvoiceNumberLength + 1), TestIssuedAt);

        Assert.True(result.IsError);
        Assert.Equal(InvoiceErrors.InvalidInvoiceNumber.Code, result.TopError.Code);
    }

    [Fact]
    public void Issue_WithoutLines_ShouldBeRejected()
    {
        // A draft that somehow ended up with no billable lines cannot be issued.
        var invoice = Invoice.Create(Guid.NewGuid(), 100m, null, InvoiceLifecycleStatus.Draft).Value;

        var result = invoice.Issue(TestNumber, TestIssuedAt);

        Assert.True(result.IsError);
        Assert.Equal(InvoiceErrors.NoBillableItems.Code, result.TopError.Code);
        Assert.Equal(InvoiceLifecycleStatus.Draft, invoice.LifecycleStatus);
    }

    [Fact]
    public void Issue_ThenRegisterPayment_ShouldSucceed()
    {
        var invoice = DraftInvoice(("Cleaning", 80m));
        invoice.Issue(TestNumber, TestIssuedAt);

        var result = invoice.RegisterPayment(80m);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Paid, invoice.Status);
    }

    // -----------------------------------------------------------------------
    // Editing
    // -----------------------------------------------------------------------

    [Fact]
    public void UpdateLine_OnDraft_ShouldRecalculateTotalServerSide()
    {
        var invoice = DraftInvoice(("Cleaning", 80m), ("Filling", 45m));
        var lineId = invoice.Lines.First(l => l.Description == "Filling").Id;

        var result = invoice.UpdateLine(lineId, "Filling (revised)", 60m);

        Assert.True(result.IsSuccess);
        Assert.Equal(140m, invoice.TotalAmount);
        Assert.True(invoice.HasManualAdjustments);
    }

    [Fact]
    public void UpdateLine_WithNonPositiveAmount_ShouldBeRejected()
    {
        var invoice = DraftInvoice(("Cleaning", 80m));
        var lineId = invoice.Lines.First().Id;

        var result = invoice.UpdateLine(lineId, "Cleaning", 0m);

        Assert.True(result.IsError);
        Assert.Equal(InvoiceErrors.InvalidLineAmount.Code, result.TopError.Code);
        Assert.Equal(80m, invoice.TotalAmount);
    }

    [Fact]
    public void RemoveLine_OnDraft_ShouldRecalculateTotal()
    {
        var invoice = DraftInvoice(("Cleaning", 80m), ("Filling", 45m));
        var lineId = invoice.Lines.First().Id;

        var result = invoice.RemoveLine(lineId);

        Assert.True(result.IsSuccess);
        Assert.Single(invoice.Lines);
        Assert.Equal(45m, invoice.TotalAmount);
        Assert.True(invoice.HasManualAdjustments);
    }

    [Fact]
    public void UpdateLine_OnIssued_ShouldBeRejected()
    {
        var invoice = DraftInvoice(("Cleaning", 80m));
        invoice.Issue(TestNumber, TestIssuedAt);
        var lineId = invoice.Lines.First().Id;

        var result = invoice.UpdateLine(lineId, "Cleaning", 999m);

        Assert.True(result.IsError);
        Assert.Equal(InvoiceErrors.CannotModifyIssuedInvoice.Code, result.TopError.Code);
        Assert.Equal(80m, invoice.TotalAmount);
    }

    [Fact]
    public void UpdateLine_OnPartiallyPaid_ShouldBeRejected()
    {
        var invoice = DraftInvoice(("Cleaning", 80m));
        invoice.Issue(TestNumber, TestIssuedAt);
        invoice.RegisterPayment(40m);
        var lineId = invoice.Lines.First().Id;

        var result = invoice.UpdateLine(lineId, "Cleaning", 999m);

        Assert.True(result.IsError);
        Assert.Equal(InvoiceErrors.CannotModifyIssuedInvoice.Code, result.TopError.Code);
        Assert.Equal(80m, invoice.TotalAmount);
    }

    [Fact]
    public void RemoveLine_UnknownLine_ShouldBeRejected()
    {
        var invoice = DraftInvoice(("Cleaning", 80m));

        var result = invoice.RemoveLine(Guid.NewGuid());

        Assert.True(result.IsError);
        Assert.Equal(InvoiceErrors.InvoiceLineNotFound.Code, result.TopError.Code);
    }

    // -----------------------------------------------------------------------
    // Refresh policy
    // -----------------------------------------------------------------------

    [Fact]
    public void ApplyLineChanges_WithoutIds_ShouldReplaceWholeSetAndFlagManualEdits()
    {
        var invoice = DraftInvoice(("Cleaning", 80m));
        var lineId = invoice.Lines.First().Id;
        invoice.UpdateLine(lineId, "Cleaning (edited)", 90m);
        Assert.True(invoice.HasManualAdjustments);

        var result = invoice.ApplyLineChanges(
        [
            (null, "Cleaning", 80m),
            (null, "Filling", 30m),
        ]);

        Assert.True(result.IsSuccess);
        Assert.Equal(110m, invoice.TotalAmount);
        Assert.Equal(2, invoice.Lines.Count);
        Assert.Equal(2, result.Value.Added.Count);
        Assert.Single(result.Value.Removed);
    }

    [Fact]
    public void ApplyLineChanges_WithKnownId_ShouldUpdateInPlaceAndKeepIdentifier()
    {
        var invoice = DraftInvoice(("Cleaning", 80m), ("Filling", 30m));
        var cleaningId = invoice.Lines.First(l => l.Description == "Cleaning").Id;
        var fillingId = invoice.Lines.First(l => l.Description == "Filling").Id;

        var result = invoice.ApplyLineChanges(
        [
            (cleaningId, "Teeth Cleaning", 70m),
            (fillingId, "Filling", 30m),
        ]);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Added);
        Assert.Empty(result.Value.Removed);
        Assert.Equal(100m, invoice.TotalAmount);
        Assert.Equal(2, invoice.Lines.Count);
        Assert.Contains(invoice.Lines, l => l.Id == cleaningId && l.Amount == 70m);
    }

    [Fact]
    public void ApplyLineChanges_WhenFewerLines_ShouldRemoveSurplus()
    {
        var invoice = DraftInvoice(("Cleaning", 80m), ("Filling", 30m));
        var cleaningId = invoice.Lines.First(l => l.Description == "Cleaning").Id;

        var result = invoice.ApplyLineChanges([(cleaningId, "Cleaning", 80m)]);

        Assert.True(result.IsSuccess);
        Assert.Single(invoice.Lines);
        Assert.Equal(80m, invoice.TotalAmount);
        Assert.Empty(result.Value.Added);
        Assert.Single(result.Value.Removed);
    }

    [Fact]
    public void ApplyLineChanges_WithNoPricedLines_ShouldBeRejected()
    {
        var invoice = DraftInvoice(("Cleaning", 80m));

        var result = invoice.ApplyLineChanges([(null, "Free consultation", 0m)]);

        Assert.True(result.IsError);
        Assert.Equal(InvoiceErrors.NoBillableItems.Code, result.TopError.Code);
        Assert.Equal(80m, invoice.TotalAmount);
    }

    [Fact]
    public void EnsureEditable_OnIssued_ShouldBeRejected()
    {
        var invoice = DraftInvoice(("Cleaning", 80m));
        invoice.Issue(TestNumber, TestIssuedAt);

        var result = invoice.EnsureEditable();

        Assert.True(result.IsError);
        Assert.Equal(InvoiceErrors.CannotModifyIssuedInvoice.Code, result.TopError.Code);
    }

    [Fact]
    public void ApplyLineChanges_OnIssued_ShouldBeRejected()
    {
        var invoice = DraftInvoice(("Cleaning", 80m));
        invoice.Issue(TestNumber, TestIssuedAt);

        var result = invoice.ApplyLineChanges([(null, "Something else", 10m)]);

        Assert.True(result.IsError);
        Assert.Equal(InvoiceErrors.CannotModifyIssuedInvoice.Code, result.TopError.Code);
        Assert.Equal(80m, invoice.TotalAmount);
    }

    // -----------------------------------------------------------------------
    // Price snapshots
    // -----------------------------------------------------------------------

    [Fact]
    public void AddLineImmediate_ShouldStoreTheAmountAsASnapshot()
    {
        var invoice = Invoice.Create(Guid.NewGuid(), 30m, null, InvoiceLifecycleStatus.Draft).Value;

        invoice.AddLineImmediate("Teeth Cleaning", 30m);

        var line = Assert.Single(invoice.Lines);
        Assert.Equal("Teeth Cleaning", line.Description);
        Assert.Equal(30m, line.Amount);
    }
}