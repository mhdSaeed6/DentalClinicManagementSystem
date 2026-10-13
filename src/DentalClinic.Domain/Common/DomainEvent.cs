using MediatR;

namespace DentalClinic.Domain.Common.Results;

public abstract record DomainEvent : INotification;
