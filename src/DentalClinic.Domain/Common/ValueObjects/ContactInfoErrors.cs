using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Domain.Common.ValueObjects;

public static class ContactInfoErrors
{
    public static readonly Error InvalidPhoneNumber = Error.Validation(
        "Common.InvalidPhoneNumber",
        "The phone number provided is invalid.");
}