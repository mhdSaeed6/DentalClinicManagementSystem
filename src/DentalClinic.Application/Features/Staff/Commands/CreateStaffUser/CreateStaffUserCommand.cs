using DentalClinic.Application.Features.Identity.Dtos;
using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.Staff.Commands.CreateStaffUser;

public sealed record CreateStaffUserCommand(
    string Email,
    string Password,
    IReadOnlyList<string> Roles) : IRequest<Result<StaffUserDto>>;