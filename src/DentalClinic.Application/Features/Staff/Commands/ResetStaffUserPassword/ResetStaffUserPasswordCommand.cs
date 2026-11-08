using DentalClinic.Application.Features.Identity.Dtos;
using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.Staff.Commands.ResetStaffUserPassword;

public sealed record ResetStaffUserPasswordCommand(
    string UserId,
    string NewPassword) : IRequest<Result<StaffUserDto>>;