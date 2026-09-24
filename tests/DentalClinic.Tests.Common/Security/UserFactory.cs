using DentalClinic.Infrastructure.Identity;

namespace DentalClinic.Tests.Common.Security;

internal class UserFactory
{
    public static AppUser CreateUser()
    {
        return new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "user@dentalclinic.local",
            UserName = "user@dentalclinic.local",
            EmailConfirmed = true
        };
    }
}