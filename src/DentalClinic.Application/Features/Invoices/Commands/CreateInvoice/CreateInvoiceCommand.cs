using DentalClinic.Application.Features.Invoices.Dtos;
using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.Invoices.Commands.CreateInvoice;

public sealed record CreateInvoiceCommand(
    Guid PatientId,
    decimal TotalAmount,
    Guid? AppointmentId) : IRequest<Result<InvoiceDto>>;
