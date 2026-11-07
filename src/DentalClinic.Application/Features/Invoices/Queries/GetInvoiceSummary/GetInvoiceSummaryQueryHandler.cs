using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Invoices.Dtos;
using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.Invoices.Queries.GetInvoiceSummary;

public class GetInvoiceSummaryQueryHandler(IAppDbContext context)
    : IRequestHandler<GetInvoiceSummaryQuery, Result<InvoiceSummaryDto>>
{
    public async Task<Result<InvoiceSummaryDto>> Handle(
        GetInvoiceSummaryQuery request,
        CancellationToken cancellationToken)
    {
        // Scope, lifecycle filter and the sums all live in the shared reader so
        // the billing screen and the reports agree by construction.
        var query = InvoiceSummaryReader.Issued(context, request.Scope, request.PatientId);

        return await InvoiceSummaryReader.ReadAsync(query, cancellationToken);
    }
}