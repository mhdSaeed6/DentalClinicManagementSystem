using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Features.Identity.Dtos;
using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.Staff.Queries.GetStaffUsers;

public sealed record GetStaffUsersQuery(
    string? SearchTerm,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<Result<PaginatedList<StaffUserDto>>>;