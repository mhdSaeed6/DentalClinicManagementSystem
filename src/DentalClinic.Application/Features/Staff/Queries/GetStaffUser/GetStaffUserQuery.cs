using DentalClinic.Application.Features.Identity.Dtos;
using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.Staff.Queries.GetStaffUser;

public sealed record GetStaffUserQuery(string UserId) : IRequest<Result<StaffUserDto>>;