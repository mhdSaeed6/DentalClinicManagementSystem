using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Infrastructure.Identity;

namespace DentalClinic.Tests.Common.Security;

public class TestCurrentUser : IUser
{
    private AppUser? _currentUser;

    public void Returns(AppUser currentUser)
    {
        _currentUser = currentUser;
    }

    public string? Id => _currentUser!.Id ?? UserFactory.CreateUser().Id;
}