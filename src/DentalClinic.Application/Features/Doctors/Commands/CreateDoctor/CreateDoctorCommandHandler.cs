using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Doctors.Dtos;
using DentalClinic.Application.Features.Doctors.Mappers;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Doctors;

using MediatR;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.Doctors.Commands.CreateDoctor;

public class CreateDoctorCommandHandler(
    ILogger<CreateDoctorCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache) :
    IRequestHandler<CreateDoctorCommand, Result<DoctorDto>>
{
    public async Task<Result<DoctorDto>> Handle(CreateDoctorCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating new doctor: {FirstName} {LastName}", request.FirstName, request.LastName);

        var doctorResult = Doctor.Create(
            request.FirstName,
            request.LastName,
            request.Specialization,
            request.ContactInfo,
            request.Gender);

        if (doctorResult.IsError)
        {
            logger.LogWarning(
                "Failed to create doctor: {FirstName} {LastName}. Errors: {@Errors}",
                request.FirstName,
                request.LastName,
                doctorResult.Errors);

            return doctorResult.Errors;
        }

        var doctor = doctorResult.Value;

        context.Doctors.Add(doctor);
        await context.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync("doctor", cancellationToken);

        logger.LogInformation("Doctor created successfully with ID: {DoctorId}", doctor.Id);

        return doctor.ToDto();
    }
}
