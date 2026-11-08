using DentalClinic.Application.Features.Imports.Dtos;
using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.Imports.Commands.PreviewPatientImport;

/// <summary>
/// Dry run: parses and validates the whole workbook without writing anything.
/// </summary>
public sealed record PreviewPatientImportCommand(
    string FileName,
    long Length,
    Stream Content,
    string? MappingJson) : IRequest<Result<PatientImportPreviewDto>>;