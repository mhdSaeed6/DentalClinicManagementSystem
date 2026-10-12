using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Domain.Identity;
using DentalClinic.Infrastructure.Identity;

namespace DentalClinic.Tests.Common.Security;

/// <summary>
/// Test double for <see cref="IUser"/>. Defaults to the <c>Admin</c> role so that
/// existing tests keep the privileged behaviour they previously assumed; use the
/// <c>As...</c> helpers to simulate doctors and receptionists.
/// </summary>
public class TestCurrentUser : IUser
{
    private readonly HashSet<string> _roles = new(StringComparer.OrdinalIgnoreCase);
    private readonly AppUser _defaultUser = UserFactory.CreateUser();
    private AppUser? _currentUser;

    public TestCurrentUser()
    {
        _roles.Add(Roles.Admin);
    }

    public Guid? DoctorId { get; private set; }

    public void Returns(AppUser currentUser) => _currentUser = currentUser;

    public TestCurrentUser AsAdmin()
    {
        SetRoles(Roles.Admin);

        return this;
    }

    public TestCurrentUser AsReceptionist()
    {
        SetRoles(Roles.Receptionist);

        return this;
    }

    public TestCurrentUser AsDoctor(Guid doctorId)
    {
        SetRoles(Roles.Doctor);
        DoctorId = doctorId;

        return this;
    }

    public TestCurrentUser WithRoles(params string[] roles)
    {
        SetRoles(roles);

        return this;
    }

    public string? Id => _currentUser?.Id ?? _defaultUser.Id;

    public bool IsInRole(string role) => _roles.Contains(role);

    private void SetRoles(params string[] roles)
    {
        _roles.Clear();
        _roles.UnionWith(roles);

        if (!_roles.Contains(Roles.Doctor))
        {
            DoctorId = null;
        }
    }
}
