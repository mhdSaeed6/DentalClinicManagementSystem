using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.DentalServices.Commands.RemoveDentalService;

public sealed record RemoveDentalServiceCommand(Guid DentalServiceId) : IRequest<Result<Deleted>>;
