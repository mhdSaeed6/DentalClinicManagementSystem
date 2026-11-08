using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Imports.Dtos;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Identity;
using DentalClinic.Domain.Imports;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace DentalClinic.Application.Features.Imports.Queries.GetImportJobErrors;

public sealed record GetImportJobErrorsQuery(Guid Id) : IRequest<Result<ImportErrorReport>>;

public class GetImportJobErrorsQueryHandler(IAppDbContext context, IUser user)
    : IRequestHandler<GetImportJobErrorsQuery, Result<ImportErrorReport>>
{
    public async Task<Result<ImportErrorReport>> Handle(
        GetImportJobErrorsQuery request,
        CancellationToken cancellationToken)
    {
        if (!user.IsInRole(Roles.Admin))
        {
            return ImportErrors.AdminOnly;
        }

        var job = await context.ImportJobs
            .AsNoTracking()
            .Include(job => job.Errors)
            .FirstOrDefaultAsync(job => job.Id == request.Id, cancellationToken);

        if (job is null)
        {
            return ImportErrors.JobNotFound;
        }

        return new ImportErrorReport(
            ImportErrorCsv.Build(job, job.Errors),
            ImportErrorCsv.FileNameFor(job));
    }
}