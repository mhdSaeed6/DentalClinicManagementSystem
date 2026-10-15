using DentalClinic.Application.Features.Invoices.Dtos;
using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.Invoices.Commands.GenerateAppointmentInvoice;

/// <summary>
/// Generates the invoice for a completed appointment from its service charge
/// and its performed treatments, or returns the existing draft when one has
/// already been generated. Idempotent: repeated requests do not duplicate.
/// </summary>
public sealed record GenerateAppointmentInvoiceCommand(
    Guid AppointmentId) : IRequest<Result<GenerateAppointmentInvoiceResult>>;
