using DentalClinic.Domain.Invoices;
using DentalClinic.Domain.Invoices.Enums;

namespace DentalClinic.Domain.UnitTests.Invoices;

/// <summary>
/// الطريقة والمرجع بيانات وصفية فقط: هذه الاختبارات تثبت أنهما يُخزَّنان
/// ويُنظَّفان دون أن يمسّا المبلغ أو أي حساب مالي.
/// </summary>
public class PaymentTest
{
    [Fact]
    public void Create_ShouldDefaultToCash_WhenMethodAndReferenceAreOmitted()
    {
        // Arrange / Act
        var result = Payment.Create(Guid.NewGuid(), 100m);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentMethod.Cash, result.Value.Method);
        Assert.Null(result.Value.Reference);
        Assert.Equal(100m, result.Value.Amount);
    }

    [Fact]
    public void Create_ShouldStoreMethodAndReference_WhenProvided()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();

        // Act
        var result = Payment.Create(
            invoiceId,
            250m,
            "Card terminal #2",
            PaymentMethod.Card,
            " **** 4242 ");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(invoiceId, result.Value.InvoiceId);
        Assert.Equal(250m, result.Value.Amount);
        Assert.Equal(PaymentMethod.Card, result.Value.Method);
        Assert.Equal("**** 4242", result.Value.Reference);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldNormaliseBlankReferenceToNull(string reference)
    {
        // Act
        var result = Payment.Create(Guid.NewGuid(), 50m, null, PaymentMethod.BankTransfer, reference);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Reference);
    }

    [Fact]
    public void Create_ShouldReject_ReferenceLongerThan64Chars()
    {
        // Arrange
        var reference = new string('R', Payment.MaxReferenceLength + 1);

        // Act
        var result = Payment.Create(Guid.NewGuid(), 50m, null, PaymentMethod.BankTransfer, reference);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal(InvoiceErrors.InvalidPaymentReference.Code, result.TopError.Code);
    }

    [Fact]
    public void Create_ShouldAccept_ReferenceOfExactly64Chars()
    {
        // Arrange
        var reference = new string('R', Payment.MaxReferenceLength);

        // Act
        var result = Payment.Create(Guid.NewGuid(), 50m, null, PaymentMethod.Other, reference);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(reference, result.Value.Reference);
    }
}