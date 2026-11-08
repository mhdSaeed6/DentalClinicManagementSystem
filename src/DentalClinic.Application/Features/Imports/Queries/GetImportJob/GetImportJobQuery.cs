using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Imports.Dtos;
using DentalClinic.Application.Features.Imports.Mappers;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Identity;
using DentalClinic.Domain.Imports;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace DentalClinic.Application.Features.Imports.Queries.GetImportJob;

public sealed record GetImportJobQuery(Guid Id) : IRequest<Result<ImportJobDto>>;

public class GetImportJobQueryHandler(IAppDbContext context, IUser user)
    : IRequestHandler<GetImportJobQuery, Result<ImportJobDto>>
{
    public async Task<Result<ImportJobDto>> Handle(GetImportJobQuery request, CancellationToken cancellationToken)
    {
        if (!user.IsInRole(Roles.Admin))
        {
            return ImportErrors.AdminOnly;
        }

        var job = await context.ImportJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(job => job.Id == request.Id, cancellationToken);

        return job is null
            ? ImportErrors.JobNotFound
            : job.ToDto();
    }
}