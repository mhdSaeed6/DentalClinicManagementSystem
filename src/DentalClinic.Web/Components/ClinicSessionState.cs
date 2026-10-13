using DentalClinic.Web.Api;

namespace DentalClinic.Web.Components;

/// <summary>
/// Single source of truth for the current browser session: authentication
/// state, tokens and UI preferences (language, theme, navigation).
/// Registered as a scoped service so each user circuit gets its own instance.
/// </summary>
public sealed class ClinicSessionState
{
    public event Action? Changed;

    // ---------------------------------------------------------------- Auth

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(AccessToken);

    public string? AccessToken { get; private set; }

    public string? RefreshToken { get; private set; }

    public DateTime ExpiresOnUtc { get; private set; }

    public string UserName { get; private set; } = string.Empty;

    public string UserEmail { get; private set; } = string.Empty;

    public IReadOnlyList<string> Roles { get; private set; } = [];

    /// <summary>Linked doctor profile id, when the signed-in account is a doctor.</summary>
    public Guid? DoctorId { get; private set; }

    public StaffRole Role
    {
        get
        {
            if (Roles.Any(role => role.Equals("Admin", StringComparison.OrdinalIgnoreCase)))
            {
                return StaffRole.Admin;
            }

            if (Roles.Any(role => role.Equals("Doctor", StringComparison.OrdinalIgnoreCase)))
            {
                return StaffRole.Doctor;
            }

            if (Roles.Any(role => role.Equals("Receptionist", StringComparison.OrdinalIgnoreCase)))
            {
                return StaffRole.Receptionist;
            }

            return StaffRole.Unknown;
        }
    }

    public bool IsDoctor => Role == StaffRole.Doctor;

    // ------------------------------------------------------------ UI prefs

    public AppLanguage Language { get; private set; } = AppLanguage.English;

    public AppTheme Theme { get; private set; } = AppTheme.Light;

    public bool SidebarCollapsed { get; private set; }

    public bool MobileNavOpen { get; private set; }

    public bool IsRtl => Language == AppLanguage.Arabic;

    public string Dir => IsRtl ? "rtl" : "ltr";

    public string ThemeName => Theme == AppTheme.Dark ? "dark" : "light";

    // -------------------------------------------------------------- Methods

    public void SetTokens(TokenResponse tokens)
    {
        AccessToken = tokens.AccessToken;
        RefreshToken = tokens.RefreshToken;
        ExpiresOnUtc = tokens.ExpiresOnUtc;
        NotifyChanged();
    }

    public void SetUser(AppUserDto user)
    {
        UserEmail = user.Email;
        UserName = string.IsNullOrWhiteSpace(user.Email) ? "User" : user.Email.Split('@')[0];
        Roles = user.Roles;
        DoctorId = user.DoctorId;
        NotifyChanged();
    }

    public void SignOut()
    {
        AccessToken = null;
        RefreshToken = null;
        ExpiresOnUtc = default;
        UserName = string.Empty;
        UserEmail = string.Empty;
        Roles = [];
        DoctorId = null;
        MobileNavOpen = false;
        NotifyChanged();
    }

    public void ToggleLanguage()
    {
        Language = Language == AppLanguage.Arabic ? AppLanguage.English : AppLanguage.Arabic;
        NotifyChanged();
    }

    public void SetLanguage(AppLanguage language)
    {
        Language = language;
        NotifyChanged();
    }

    public void ToggleTheme()
    {
        Theme = Theme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark;
        NotifyChanged();
    }

    public void ToggleSidebar()
    {
        SidebarCollapsed = !SidebarCollapsed;
        NotifyChanged();
    }

    public void ToggleMobileNav()
    {
        MobileNavOpen = !MobileNavOpen;
        NotifyChanged();
    }

    public void CloseMobileNav()
    {
        if (!MobileNavOpen)
        {
            return;
        }

        MobileNavOpen = false;
        NotifyChanged();
    }

    public void Rebind(Action handler) => Changed += handler;

    public void Unbind(Action handler) => Changed -= handler;

    /// <summary>
    /// Tiny inline localization helper: returns the Arabic string when the
    /// session language is Arabic, otherwise the English one.
    /// </summary>
    public string T(string english, string arabic) => IsRtl ? arabic : english;

    private void NotifyChanged() => Changed?.Invoke();
}

public enum AppLanguage
{
    English,
    Arabic,
}

public enum AppTheme
{
    Light,
    Dark,
}

public enum StaffRole
{
    Admin,
    Doctor,
    Receptionist,
    Unknown,
}
