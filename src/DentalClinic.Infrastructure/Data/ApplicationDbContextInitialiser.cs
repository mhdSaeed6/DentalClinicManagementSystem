using DentalClinic.Domain.Appointments;
using DentalClinic.Domain.Appointments.Enums;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Doctors;
using DentalClinic.Domain.Identity;
using DentalClinic.Domain.Patients;
using DentalClinic.Domain.Services;
using DentalClinic.Infrastructure.Identity;
using DentalClinic.Infrastructure.Settings;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DentalClinic.Infrastructure.Data;

public class ApplicationDbContextInitialiser(
    ILogger<ApplicationDbContextInitialiser> logger,
    AppDbContext context,
    UserManager<AppUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptions<AppSettings> appSettingsOptions)
{
    private readonly ILogger<ApplicationDbContextInitialiser> _logger = logger;
    private readonly AppDbContext _context = context;
    private readonly UserManager<AppUser> _userManager = userManager;
    private readonly RoleManager<IdentityRole> _roleManager = roleManager;
    private readonly AppSettings _appSettings = appSettingsOptions.Value;

    public async Task InitialiseAsync()
    {
        try
        {
            await _context.Database.EnsureCreatedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initialising the database.");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    public async Task TrySeedAsync()
    {
        // 1. Roles Definition
        string[] roles = ["Admin", "Doctor", "Receptionist"];
        foreach (var roleName in roles)
        {
            if (_roleManager.Roles.All(r => r.Name != roleName))
            {
                await _roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // 2. Default Admin Account
        var adminUser = new AppUser
        {
            Id = "19a59129-6c20-417a-834d-11a208d32d96",
            Email = "admin@dentalclinic.com",
            UserName = "admin@dentalclinic.com",
            EmailConfirmed = true
        };

        if (_userManager.Users.All(u => u.Email != adminUser.Email))
        {
            await _userManager.CreateAsync(adminUser, "Admin123!");
            await _userManager.AddToRoleAsync(adminUser, "Admin");
        }

        // 3. Doctors Accounts & Profiles
        var doctor01User = new AppUser
        {
            Id = "b6327240-0aea-46fc-863a-777fc4e42560",
            Email = "dr.smith@dentalclinic.com",
            UserName = "dr.smith@dentalclinic.com",
            EmailConfirmed = true
        };

        if (_userManager.Users.All(u => u.Email != doctor01User.Email))
        {
            await _userManager.CreateAsync(doctor01User, "Doctor123!");
            await _userManager.AddToRoleAsync(doctor01User, "Doctor");
        }

        var doctor02User = new AppUser
        {
            Id = "8104ab20-26c2-4651-b1de-c0baf04dbbd9",
            Email = "dr.sara@dentalclinic.com",
            UserName = "dr.sara@dentalclinic.com",
            EmailConfirmed = true
        };

        if (_userManager.Users.All(u => u.Email != doctor02User.Email))
        {
            await _userManager.CreateAsync(doctor02User, "Doctor123!");
            await _userManager.AddToRoleAsync(doctor02User, "Doctor");
        }

        // 4. Seed Doctors
        if (!await _context.Doctors.AnyAsync())
        {
            var contact1Res = ContactInfo.Create("+963911111111");
            var contact2Res = ContactInfo.Create("+963922222222");

            if (contact1Res.IsError || contact2Res.IsError)
            {
                throw new Exception("ContactInfo Seeding Failed.");
            }

            var doc1Res = Doctor.Create("John", "Smith", "Orthodontics", contact1Res.Value, Gender.Male);
            var doc2Res = Doctor.Create("Sara", "Johnson", "Endodontics", contact2Res.Value, Gender.Female);

            if (doc1Res.IsError || doc2Res.IsError)
            {
                throw new Exception("Doctor Seeding Failed.");
            }

            // <-- هذا السطر كان مفقوداً لديكم
            _context.Doctors.AddRange([doc1Res.Value, doc2Res.Value]);
        }

        // 5. Seed Dental Services
        if (!_context.DentalServices.Any())
        {
            _context.DentalServices.AddRange([
                DentalService.Create("Teeth Cleaning", "Routine cleaning and polishing", 50.00m).Value,
                DentalService.Create("Composite Filling", "Tooth-colored restoration", 120.00m).Value,
                DentalService.Create("Root Canal Treatment", "Endodontic therapy", 350.00m).Value,
                DentalService.Create("Tooth Extraction", "Simple tooth removal", 100.00m).Value,
                DentalService.Create("Teeth Whitening", "Professional bleaching procedure", 200.00m).Value
            ]);
        }

        // 6. Patients Seed
        if (!_context.Patients.Any())
        { // 1. إنشاء ContactInfo
            var contact1Res = ContactInfo.Create("+963911111222");
            var contact2Res = ContactInfo.Create("+963922222333");

            if (contact1Res.IsError || contact2Res.IsError)
            {
                throw new Exception($"ContactInfo Seeding Failed: {contact1Res.Errors} | {contact2Res.Errors}");
            }

            _context.Patients.AddRange([
                Patient.Create("Michael", "Brown", contact1Res.Value, DateTime.UtcNow.AddYears(-30), Gender.Male).Value,
                Patient.Create("Emma", "Wilson", contact2Res.Value, DateTime.UtcNow.AddYears(-25), Gender.Female).Value
            ]);
        }

        await _context.SaveChangesAsync();

        // 7. Sample Appointments Seed
        if (!_context.Appointments.Any())
        {
            var doctors = await _context.Doctors.ToListAsync();
            var patients = await _context.Patients.ToListAsync();
            var services = await _context.DentalServices.ToListAsync();

            if (doctors.Count > 0 && patients.Count > 0 && services.Count > 0)
            {
                var now = DateTimeOffset.UtcNow;
                var startAt = now.AddHours(2);

                var duration = _appSettings.MinimumAppointmentDurationInMinutes > 0
                    ? _appSettings.MinimumAppointmentDurationInMinutes
                    : 30;

                var sampleAppointment = Appointment.Create(
                    patients[0].Id,
                    doctors[0].Id,
                    services[0].Id,
                    startAt.DateTime,
                    duration).Value; // <--- الاعتماد على _appSettings الحقل المحقون

                _context.Appointments.Add(sampleAppointment);
                await _context.SaveChangesAsync();
            }
        }
    }
}

public static class InitialiserExtensions
{
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();

        await initialiser.InitialiseAsync();
        await initialiser.SeedAsync();
    }
}