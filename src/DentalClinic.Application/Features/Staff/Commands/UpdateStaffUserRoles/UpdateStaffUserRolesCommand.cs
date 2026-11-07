using DentalClinic.Application.Features.Identity.Dtos;
using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.Staff.Commands.UpdateStaffUserRoles;

public sealed record UpdateStaffUserRolesCommand(
    string UserId,
    IReadOnlyList<string> Roles) : IRequest<Result<StaffUserDto>>;