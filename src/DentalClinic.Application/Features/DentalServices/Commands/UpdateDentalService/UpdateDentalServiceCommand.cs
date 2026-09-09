using DentalClinic.Application.Features.DentalServices.Dtos;
using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.DentalServices.Commands.UpdateDentalService;

public sealed record UpdateDentalServiceCommand(
    Guid DentalServiceId,
    string Name,
    string? Description,
    decimal Price) : IRequest<Result<DentalServiceDto>>;
