using DentalClinic.Application.Features.Identity.Dtos;
using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.Staff.Commands.SetStaffUserActive;

public sealed record SetStaffUserActiveCommand(
    string UserId,
    bool IsActive) : IRequest<Result<StaffUserDto>>;