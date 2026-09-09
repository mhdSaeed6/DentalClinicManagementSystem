using DentalClinic.Application.Features.TreatmentRecords.Dtos;
using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.TreatmentRecords.Commands.CreateTreatmentRecord;

public sealed record CreateTreatmentRecordCommand(
    Guid PatientId,
    Guid DoctorId,
    Guid? AppointmentId,
    int ToothNumber,
    string ProcedureDetails,
    decimal Cost) : IRequest<Result<TreatmentRecordDto>>;
