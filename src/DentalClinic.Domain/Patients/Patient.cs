using System.Text.RegularExpressions;

using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Domain.Patients;

public class Patient : AuditableEntity
{
    private static readonly Regex PhoneRegex = new(@"^\+?\d{7,15}$", RegexOptions.Compiled);

    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string PhoneNumber { get; private set; } = default!;
    public DateTime DateOfBirth { get; private set; }
    public Gender Gender { get; private set; }
    public MedicalHistory MedicalHistory { get; private set; } = default!;

    private Patient() { }

    private Patient(
        Guid id,
        string firstName,
        string lastName,
        string phoneNumber,
        DateTime dateOfBirth,
        Gender gender,
        MedicalHistory medicalHistory)
        : base(id)
    {
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
        DateOfBirth = dateOfBirth;
        Gender = gender;
        MedicalHistory = medicalHistory;
    }

    public static Result<Patient> Create(
        string firstName,
        string lastName,
        string phoneNumber,
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

        if (string.IsNullOrWhiteSpace(phoneNumber) || !PhoneRegex.IsMatch(phoneNumber))
        {
            return PatientErrors.InvalidPhoneNumber;
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
            phoneNumber.Trim(),
            dateOfBirth,
            gender,
            medicalHistory ?? new MedicalHistory());
    }

    public Result<Updated> Update(
        string firstName,
        string lastName,
        string phoneNumber,
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

        if (string.IsNullOrWhiteSpace(phoneNumber) || !PhoneRegex.IsMatch(phoneNumber))
        {
            return PatientErrors.InvalidPhoneNumber;
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
        PhoneNumber = phoneNumber.Trim();
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
