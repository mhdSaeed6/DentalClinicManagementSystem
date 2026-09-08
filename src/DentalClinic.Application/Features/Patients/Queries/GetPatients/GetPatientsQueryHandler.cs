using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Patients.Dtos;
using DentalClinic.Application.Features.Patients.Mappers;
using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace DentalClinic.Application.Features.Patients.Queries.GetPatients;

public class GetPatientsQueryHandler(IAppDbContext context) : IRequestHandler<GetPatientsQuery, Result<List<PatientDto>>>
{
    public async Task<Result<List<PatientDto>>> Handle(GetPatientsQuery request, CancellationToken cancellationToken)
    {
        var patients = await context.Patients
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return patients.ToDtos();
    }
}