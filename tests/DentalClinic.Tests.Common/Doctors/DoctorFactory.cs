using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Doctors;

namespace DentalClinic.Tests.Common.Doctors;

public static class DoctorFactory
{
    public static Result<Doctor> CreateDoctor(
        string firstName = "John",
        string lastName = "Doe",
        string specialization = "General Dentistry",
        ContactInfo? contactInfo = null,
        Gender? gender = Gender.Male)
    {
        // ملاحظة: إذا كانت ContactInfo إلزامية في الـ Domain، يمكنك إنشاء قيمة وهمية افتراضية لها هنا
        return Doctor.Create(
            firstName,
            lastName,
            specialization,
            contactInfo ?? ContactInfo.Create("741085207410", null, true, "koko.com").Value, // قيمة افتراضية لتفادي الـ NullReference
            gender ?? Gender.Male);
    }
}