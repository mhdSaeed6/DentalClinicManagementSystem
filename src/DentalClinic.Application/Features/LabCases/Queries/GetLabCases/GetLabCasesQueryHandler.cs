using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Features.LabCases.Dtos;
using DentalClinic.Application.Features.LabCases.Mappers;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Labs;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace DentalClinic.Application.Features.LabCases.Queries.GetLabCases;

public class GetLabCasesQueryHandler(IAppDbContext context)
    : IRequestHandler<GetLabCasesQuery, Result<PaginatedList<LabCaseDto>>>
{
    public async Task<Result<PaginatedList<LabCaseDto>>> Handle(GetLabCasesQuery request, CancellationToken cancellationToken)
    {
        // Overdue is derived at query time from the expected return date; the
        // same clock value is used for filtering and for the response so the
        // rows and their IsOverdue flag can never disagree.
        var asOfUtc = request.AsOfUtc ?? DateTimeOffset.UtcNow;

        var query = context.LabCases.AsNoTracking();

        // Trusted, server-derived scope: a doctor only sees their own lab cases.
        if (request.Scope.IsRestricted)
        {
            var scopeDoctorId = request.Scope.DoctorId;
            query = query.Where(c => c.DoctorId == scopeDoctorId);
        }

        if (request.PatientId.HasValue)
        {
            query = query.Where(c => c.PatientId == request.PatientId.Value);
        }

        if (request.DoctorId.HasValue)
        {
            query = query.Where(c => c.DoctorId == request.DoctorId.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(c => c.Status == request.Status.Value);
        }

        if (request.Overdue == true)
        {
            query = query.Where(c =>
                c.Status != LabCaseStatus.Returned &&
                c.Status != LabCaseStatus.Failed &&
                c.ExpectedReturnAtUtc < asOfUtc);
        }
        else if (request.Overdue == false)
        {
            query = query.Where(c =>
                c.Status == LabCaseStatus.Returned ||
                c.Status == LabCaseStatus.Failed ||
                c.ExpectedReturnAtUtc >= asOfUtc);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var labCases = await query
            .OrderBy(c => c.ExpectedReturnAtUtc)
            .ThenByDescending(c => c.CreatedAtUtc)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // Resolve display names in two round-trips so the UI never renders a
        // bare Guid for the patient or the doctor.
        var patientIds = labCases.Select(c => c.PatientId).Distinct().ToList();
        var patientNames = patientIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await context.Patients
                .Where(p => patientIds.Contains(p.Id))
                .Select(p => new { p.Id, Name = p.FirstName + " " + p.LastName })
                .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

        var doctorIds = labCases.Select(c => c.DoctorId).Distinct().ToList();
        var doctorNames = doctorIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await context.Doctors
                .Where(d => doctorIds.Contains(d.Id))
                .Select(d => new { d.Id, Name = d.FirstName + " " + d.LastName })
                .ToDictionaryAsync(d => d.Id, d => d.Name, cancellationToken);

        return new PaginatedList<LabCaseDto>
        {
            Items = labCases.ToDtos(
                asOfUtc,
                c => patientNames.GetValueOrDefault(c.PatientId),
                c => doctorNames.GetValueOrDefault(c.DoctorId)),
            TotalCount = totalCount,
            TotalPages = request.PageSize > 0 ? (int)Math.Ceiling(totalCount / (double)request.PageSize) : 0,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}