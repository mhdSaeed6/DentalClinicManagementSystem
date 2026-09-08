using DentalClinic.Application.Features.Patients.Dtos;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Patients;

using MediatR;

namespace DentalClinic.Application.Features.Patients.Commands.UpdatePatient;

public sealed record UpdatePatientCommand(
    Guid PatientId,
    string FirstName,
    string LastName,
    ContactInfo ContactInfo,
    DateTime DateOfBirth,
    int Gender,
    MedicalHistory MedicalHistory) : IRequest<Result<PatientDto>>;