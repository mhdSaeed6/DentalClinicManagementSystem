using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Services;

namespace DentalClinic.Tests.Common.DentalServices
{
    public static class DentalServiceFactory
    {
        public static Result<DentalService> CreateDentalService(
            string name = "Teeth Cleaning",
            string? description = null,
            decimal price = 100.0m)
        {
            return DentalService.Create(name, description, price);
        }
    }
}