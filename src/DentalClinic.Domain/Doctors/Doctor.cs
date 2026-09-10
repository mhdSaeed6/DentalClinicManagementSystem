using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.Interfaces;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Common.ValueObjects;

namespace DentalClinic.Domain.Doctors;

public class Doctor : AuditableEntity
{
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string Specialization { get; private set; } = default!;
    public ContactInfo ContactInfo { get; private set; } = default!;
    public Gender Gender { get; private set; }
    public bool IsActive { get; private set; }

    private Doctor() { }

    private Doctor(
        Guid id,
        string firstName,
        string lastName,
        string specialization,
        ContactInfo contactInfo,
        Gender gender)
        : base(id)
    {
        FirstName = firstName;
        LastName = lastName;
        Specialization = specialization;
        ContactInfo = contactInfo;
        Gender = gender;
        IsActive = true;
    }

    public static Result<Doctor> Create(
        string firstName,
        string lastName,
        string specialization,
        ContactInfo contactInfo,
        Gender gender)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            return DoctorErrors.FirstNameRequired;
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            return DoctorErrors.LastNameRequired;
        }

        if (string.IsNullOrWhiteSpace(specialization))
        {
            return DoctorErrors.SpecializationRequired;
        }

        if (contactInfo is null)
        {
            return DoctorErrors.ContactInfoRequired;
        }

        if (!Enum.IsDefined(gender))
        {
            return DoctorErrors.GenderRequired;
        }

        return new Doctor(
            Guid.NewGuid(),
            firstName.Trim(),
            lastName.Trim(),
            specialization.Trim(),
            contactInfo,
            gender);
    }

    public Result<Updated> Update(
        string firstName,
        string lastName,
        string specialization,
        ContactInfo contactInfo,
        Gender gender)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            return DoctorErrors.FirstNameRequired;
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            return DoctorErrors.LastNameRequired;
        }

        if (string.IsNullOrWhiteSpace(specialization))
        {
            return DoctorErrors.SpecializationRequired;
        }

        if (contactInfo is null)
        {
            return DoctorErrors.ContactInfoRequired;
        }

        if (!Enum.IsDefined(gender))
        {
            return DoctorErrors.GenderRequired;
        }

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Specialization = specialization.Trim();
        ContactInfo = contactInfo;
        Gender = gender;

        return Result.Updated;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}