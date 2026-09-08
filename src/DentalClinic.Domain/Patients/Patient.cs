using System.Text.RegularExpressions;

using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.Interfaces;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Common.ValueObjects;

namespace DentalClinic.Domain.Patients;

public class Patient : AuditableEntity, ISoftDeletable
{
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public ContactInfo ContactInfo { get; private set; } = default!;
    public DateTime DateOfBirth { get; private set; }
    public Gender Gender { get; private set; }
    public MedicalHistory MedicalHistory { get; private set; } = default!;

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    public void Delete(string? deletedBy = null)
    {
        IsDeleted = true;
        DeletedAtUtc = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy?.Trim();
    }

    private Patient() { }

    private Patient(
        Guid id,
        string firstName,
        string lastName,
        ContactInfo contactInfo,
        DateTime dateOfBirth,
        Gender gender,
        MedicalHistory medicalHistory)
        : base(id)
    {
        FirstName = firstName;
        LastName = lastName;
        ContactInfo = contactInfo;
        DateOfBirth = dateOfBirth;
        Gender = gender;
        MedicalHistory = medicalHistory;
    }

    public static Result<Patient> Create(
        string firstName,
        string lastName,
        ContactInfo contactInfo,
        DateTime dateOfBirth,
        Gender gender,
        MedicalHistory? medicalHistory = null)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            return PatientErrors.FirstNameRequired;
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            return PatientErrors.LastNameRequired;
        }

        if (contactInfo is null)
        {
            return PatientErrors.ContactInfoRequired;
        }

        if (DateOfBirthValid(dateOfBirth) == false)
        {
            return PatientErrors.InvalidDateOfBirth;
        }

        if (!Enum.IsDefined(gender))
        {
            return PatientErrors.GenderRequired;
        }

        return new Patient(
            Guid.NewGuid(),
            firstName.Trim(),
            lastName.Trim(),
            contactInfo,
            dateOfBirth,
            gender,
            medicalHistory ?? new MedicalHistory());
    }

    public Result<Updated> Update(
        string firstName,
        string lastName,
        ContactInfo contactInfo,
        DateTime dateOfBirth,
        Gender gender,
        MedicalHistory? medicalHistory = null)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            return PatientErrors.FirstNameRequired;
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            return PatientErrors.LastNameRequired;
        }

        if (contactInfo is null)
        {
            return PatientErrors.ContactInfoRequired;
        }

        if (DateOfBirthValid(dateOfBirth) == false)
        {
            return PatientErrors.InvalidDateOfBirth;
        }

        if (!Enum.IsDefined(gender))
        {
            return PatientErrors.GenderRequired;
        }

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        ContactInfo = contactInfo;
        DateOfBirth = dateOfBirth;
        Gender = gender;
        MedicalHistory = medicalHistory ?? new MedicalHistory();

        return Result.Updated;
    }

    private static bool DateOfBirthValid(DateTime dateOfBirth)
    {
        return dateOfBirth <= DateTime.UtcNow && dateOfBirth >= DateTime.UtcNow.AddYears(-150);
    }
}
