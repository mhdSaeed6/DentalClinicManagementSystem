using DentalClinic.Application.Features.Imports.Dtos;
using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.Imports.Commands.CommitPatientImport;

/// <summary>
/// All-or-nothing patient import. A workbook with any rejected row persists a
/// Failed job and returns its report; a fully valid workbook inserts every
/// patient and returns a Completed report.
/// </summary>
public sealed record CommitPatientImportCommand(
    string FileName,
    long Length,
    Stream Content,
    string? MappingJson) : IRequest<Result<PatientImportReportDto>>;