using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Domain.Services;

public static class ServiceErrors
{
    public static readonly Error NameRequired = Error.Validation(
        "Service.NameRequired",
        "Service name is required.");

    public static readonly Error InvalidPrice = Error.Validation(
        "Service.InvalidPrice",
        "Price must be greater than or equal to zero.");

    public static readonly Error ServiceInactive = Error.Validation(
        "Service.ServiceInactive",
        "Cannot perform operations on an inactive service.");
}