using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.Appointments.Commands.MarkOverdueAppointmentsAsNoShow;

/// <summary>
/// Marks every scheduled appointment that has already ended (its scheduled start plus duration)
/// and is older than the supplied grace period as a no-show. Used by the background cleanup service.
/// </summary>
public sealed record MarkOverdueAppointmentsAsNoShowCommand(DateTime AsOfUtc, TimeSpan GracePeriod)
    : IRequest<Result<int>>;
