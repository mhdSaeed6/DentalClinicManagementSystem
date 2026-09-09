using DentalClinic.Application.Features.Invoices.Dtos;
using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.Invoices.Commands.AddInvoicePayment;

public sealed record AddInvoicePaymentCommand(
    Guid InvoiceId,
    decimal Amount,
    string? Notes) : IRequest<Result<InvoiceDto>>;
