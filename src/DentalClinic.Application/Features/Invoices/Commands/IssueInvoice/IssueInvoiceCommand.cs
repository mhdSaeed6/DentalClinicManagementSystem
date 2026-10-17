using DentalClinic.Application.Features.Invoices.Dtos;
using DentalClinic.Domain.Common.Results;
using MediatR;

namespace DentalClinic.Application.Features.Invoices.Commands.IssueInvoice;

public record IssueInvoiceCommand(Guid InvoiceId) : IRequest<Result<InvoiceDto>>;