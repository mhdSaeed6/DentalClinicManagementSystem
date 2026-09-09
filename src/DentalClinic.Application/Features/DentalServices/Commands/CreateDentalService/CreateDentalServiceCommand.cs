using DentalClinic.Application.Features.DentalServices.Dtos;
using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.DentalServices.Commands.CreateDentalService;

public sealed record CreateDentalServiceCommand(
    string Name,
    string? Description,
    decimal Price) : IRequest<Result<DentalServiceDto>>;
