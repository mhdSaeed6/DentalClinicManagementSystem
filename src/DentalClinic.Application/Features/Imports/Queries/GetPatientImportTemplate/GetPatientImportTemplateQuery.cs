using DentalClinic.Application.Features.Imports.Dtos;
using DentalClinic.Application.Features.Imports.Patients;
using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.Imports.Queries.GetPatientImportTemplate;

public sealed record GetPatientImportTemplateQuery : IRequest<Result<ImportTemplateFile>>;

public class GetPatientImportTemplateQueryHandler(PatientImportTemplateBuilder builder)
    : IRequestHandler<GetPatientImportTemplateQuery, Result<ImportTemplateFile>>
{
    public Task<Result<ImportTemplateFile>> Handle(
        GetPatientImportTemplateQuery request,
        CancellationToken cancellationToken) =>
        Task.FromResult<Result<ImportTemplateFile>>(builder.Build());
}