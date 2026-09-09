using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Features.Patients.Dtos;
using DentalClinic.Application.Features.Patients.Mappers;
using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace DentalClinic.Application.Features.Patients.Queries.GetPatients;

public class GetPatientsQueryHandler(IAppDbContext context) : IRequestHandler<GetPatientsQuery, Result<PaginatedList<PatientDto>>>
{
    public async Task<Result<PaginatedList<PatientDto>>> Handle(GetPatientsQuery request, CancellationToken cancellationToken)
    {
        var query = context.Patients.AsNoTracking();

        // 1. فلترة بالاسم أو رقم الهاتف إذا كان المستخدم يبحث عن مريض
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim().ToLower();
            query = query.Where(p =>
                p.FirstName.ToLower().Contains(search) ||
                p.LastName.ToLower().Contains(search) ||
                p.ContactInfo.PrimaryPhone.Contains(search) );
        }

        // 2. حساب الإجمالي وجلب الصفحة المطلوبة فقط
        var totalCount = await query.CountAsync(cancellationToken);

        var patients = await query
            .OrderByDescending(p => p.CreatedAtUtc) // الترتيب من الأحدث للأقدم
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = patients.ToDtos();

        var paginatedResult = new PaginatedList<PatientDto>()
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        return paginatedResult;
    }
}