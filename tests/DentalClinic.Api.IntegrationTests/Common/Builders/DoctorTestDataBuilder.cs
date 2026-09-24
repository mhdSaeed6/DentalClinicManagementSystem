using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Doctors;

namespace DentalClinic.Tests.Common.Builders;

public class DoctorTestDataBuilder : ITestDataBuilder<Doctor>
{
    private readonly ContactInfo _contactInfo = ContactInfo.Create("+963922222222").Value;
    private string _firstName = "Samer";
    private string _lastName = "Khouri";
    private string _specialization = "Orthodontics";

    public static DoctorTestDataBuilder Create() => new();

    public DoctorTestDataBuilder WithName(string firstName, string lastName)
    {
        _firstName = firstName;
        _lastName = lastName;
        return this;
    }

    public DoctorTestDataBuilder WithSpecialization(string specialization)
    {
        _specialization = specialization;
        return this;
    }

    public Doctor Build()
    {
        return Doctor.Create(_firstName, _lastName, _specialization, _contactInfo, Gender.Male).Value;
    }
}