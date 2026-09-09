using DentalClinic.Application.Features.Doctors.Dtos;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Common.ValueObjects;

using MediatR;

namespace DentalClinic.Application.Features.Doctors.Commands.CreateDoctor;

public sealed record CreateDoctorCommand(
    string FirstName,
    string LastName,
    string Specialization,
    ContactInfo ContactInfo,
    Gender Gender) : IRequest<Result<DoctorDto>>;
