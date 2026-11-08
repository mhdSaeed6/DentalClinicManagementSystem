using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.LabCases.Dtos;
using DentalClinic.Application.Features.LabCases.Mappers;
using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace DentalClinic.Application.Features.LabCases.Queries.GetLabCaseById;

public class GetLabCaseByIdQueryHandler(IAppDbContext context)
    : IRequestHandler<GetLabCaseByIdQuery, Result<LabCaseDto>>
{
    public async Task<Result<LabCaseDto>> Handle(GetLabCaseByIdQuery request, CancellationToken cancellationToken)
    {
        var labCase = await context.LabCases
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.LabCaseId, cancellationToken);

        if (labCase is null)
        {
            return ApplicationErrors.LabCaseNotFound;
        }

        // A restricted (doctor) scope may only read its own cases; the same
        // scope value backs the list query so the two can never diverge.
        if (request.Scope.IsRestricted && labCase.DoctorId != request.Scope.DoctorId)
        {
            return ApplicationErrors.LabCaseAccessForbidden;
        }

        var patientName = await context.Patients
            .Where(p => p.Id == labCase.PatientId)
            .Select(p => p.FirstName + " " + p.LastName)
            .FirstOrDefaultAsync(cancellationToken);

        var doctorName = await context.Doctors
            .Where(d => d.Id == labCase.DoctorId)
            .Select(d => d.FirstName + " " + d.LastName)
            .FirstOrDefaultAsync(cancellationToken);

        return labCase.ToDto(DateTimeOffset.UtcNow, patientName, doctorName);
    }
}