using DentalClinic.Domain.Appointments;
using DentalClinic.Domain.Appointments.Enums;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Doctors;
using DentalClinic.Domain.Identity;
using DentalClinic.Domain.Odontograms;
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
    // Development-only demo credentials. They satisfy the Identity password
    // policy and must stay in sync with Tests.Common/Security/TestUsers.cs.
    // Production must never reach this code path — see Program.cs, where
    // InitialiseDatabaseAsync() only runs under the Development environment.
    private const string SeededAdminPassword = "Admin!Pass123";
    private const string SeededDoctorPassword = "Doctor!Pass123";
    private const string SeededReceptionistPassword = "Receptionist!Pass123";

    private readonly ILogger<ApplicationDbContextInitialiser> _logger = logger;
    private readonly AppDbContext _context = context;
    private readonly UserManager<AppUser> _userManager = userManager;
    private readonly RoleManager<IdentityRole> _roleManager = roleManager;
    private readonly AppSettings _appSettings = appSettingsOptions.Value;

    public async Task InitialiseAsync()
    {
        try
        {
            // Migrations (not EnsureCreated) so schema changes actually reach an
            // existing database. EnsureCreated is a no-op once the database
            // exists, which left the app querying columns that were never added.
            await _context.Database.MigrateAsync();

            if (!await _context.Database.CanConnectAsync())
            {
                throw new InvalidOperationException("Cannot connect to the clinic database.");
            }
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
        var adminUser = await EnsureDemoAccountAsync(
            new AppUser
            {
                Id = "19a59129-6c20-417a-834d-11a208d32d96",
                Email = "admin@dentalclinic.com",
                UserName = "admin@dentalclinic.com",
                EmailConfirmed = true
            },
            SeededAdminPassword,
            "Admin");

        // 3. Doctors Accounts & Profiles
        var doctor01User = await EnsureDemoAccountAsync(
            new AppUser
            {
                Id = "b6327240-0aea-46fc-863a-777fc4e42560",
                Email = "dr.smith@dentalclinic.com",
                UserName = "dr.smith@dentalclinic.com",
                EmailConfirmed = true
            },
            SeededDoctorPassword,
            "Doctor");

        var doctor02User = await EnsureDemoAccountAsync(
            new AppUser
            {
                Id = "8104ab20-26c2-4651-b1de-c0baf04dbbd9",
                Email = "dr.sara@dentalclinic.com",
                UserName = "dr.sara@dentalclinic.com",
                EmailConfirmed = true
            },
            SeededDoctorPassword,
            "Doctor");

        // 3b. Receptionist Account
        var receptionistUser = await EnsureDemoAccountAsync(
            new AppUser
            {
                Id = "2f9c4a51-7b3e-4d0a-9c1e-5a6b7c8d9e0f",
                Email = "reception@dentalclinic.com",
                UserName = "reception@dentalclinic.com",
                EmailConfirmed = true
            },
            SeededReceptionistPassword,
            "Receptionist");

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

            // ربط ملفات الطبيب بحسابات Identity الخاصة بهم
            doc1Res.Value.AssignUser(doctor01User.Id);
            doc2Res.Value.AssignUser(doctor02User.Id);

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

            // Demo medical alerts so the alert badges and the dashboard count
            // are not always zero on a fresh database.
            var michaelHistory = new MedicalHistory
            {
                HasDiabetes = true,
                BloodPressureReading = "148/92"
            };

            var emmaHistory = new MedicalHistory
            {
                HasAsthma = true,
                IsPregnant = true,
                PregnancyWeeks = 32,
                BloodPressureReading = "118/76"
            };

            _context.Patients.AddRange([
                Patient.Create("Michael", "Brown", contact1Res.Value, DateTime.UtcNow.AddYears(-30), Gender.Male, michaelHistory).Value,
                Patient.Create("Emma", "Wilson", contact2Res.Value, DateTime.UtcNow.AddYears(-25), Gender.Female, emmaHistory).Value
            ]);
        }

        await _context.SaveChangesAsync();

        // 4b. Ensure existing (previously unlinked) seeded doctor profiles are linked
        //     to their identity accounts. Idempotent so re-running is safe.
        var doctorProfiles = await _context.Doctors.ToListAsync();
        var smithProfile = doctorProfiles.FirstOrDefault(d => d.FirstName == "John" && d.LastName == "Smith");
        if (smithProfile is not null && string.IsNullOrEmpty(smithProfile.UserId))
        {
            smithProfile.AssignUser(doctor01User.Id);
        }

        var saraProfile = doctorProfiles.FirstOrDefault(d => d.FirstName == "Sara" && d.LastName == "Johnson");
        if (saraProfile is not null && string.IsNullOrEmpty(saraProfile.UserId))
        {
            saraProfile.AssignUser(doctor02User.Id);
        }

        // Persist the back-filled doctor/account links before moving on; on an
        // existing database the appointment seed below is skipped, so without
        // this call the links would never be committed.
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

        // 8. Demo Odontogram Seed (S6) — بيانات عرض لحالات الأسنان (التطوير فقط مثل بقية المهيأ).
        // Demo per-tooth conditions for the seeded patients, built through the
        // PatientTooth.Create factory like every other seeded domain object.
        var demoPatients = await _context.Patients.ToListAsync();
        var demoPatientIds = demoPatients
            .Select(p => p.Id)
            .ToList();

        // الصفوف المحذوفة ناعماً تعد أيضاً "موجودة" (IgnoreQueryFilters): كي لا يعيد المهيأ إضافة سن مسحه المستخدم
        // ولن يمس الفهرس الفريد المصفى على (PatientId, ToothNumber) عند إعادة تشغيل الحاوية — مثالي التكرارية.
        // Soft-deleted rows count as "already exists" too: a restart never hits the
        // filtered unique index and never resurrects a condition the clinician cleared.
        var seededTeeth = await _context.PatientTeeth
            .IgnoreQueryFilters()
            .Where(pt => demoPatientIds.Contains(pt.PatientId))
            .Select(pt => new { pt.PatientId, pt.ToothNumber })
            .ToListAsync();

        var seededPairs = new HashSet<(Guid PatientId, int ToothNumber)>();

        foreach (var seededTooth in seededTeeth)
        {
            seededPairs.Add((seededTooth.PatientId, seededTooth.ToothNumber));
        }

        var michaelPatient = demoPatients.FirstOrDefault(p => p.FirstName == "Michael" && p.LastName == "Brown");
        var emmaPatient = demoPatients.FirstOrDefault(p => p.FirstName == "Emma" && p.LastName == "Wilson");

        // نصوص عامة للعرض فقط — لا بيانات حقيقية. Generic demo notes only — no real data.
        var demoTeeth = new (Patient? Patient, int ToothNumber, ToothCondition Condition, ToothSurface? Surface, string? Notes)[]
        {
            (michaelPatient, 16, ToothCondition.Caries, ToothSurface.Occlusal, "Demo row — occlusal caries flagged at the last check-up."),
            (michaelPatient, 26, ToothCondition.Filled, ToothSurface.Mesial, "Demo row — old composite filling, intact margins."),
            (michaelPatient, 36, ToothCondition.Crown, null, null),
            (emmaPatient, 11, ToothCondition.Missing, null, null),
            (emmaPatient, 21, ToothCondition.Healthy, ToothSurface.Incisal, "Demo row — routine review, no findings.")
        };

        var addedTeeth = 0;

        foreach (var demo in demoTeeth)
        {
            if (demo.Patient is null || seededPairs.Contains((demo.Patient.Id, demo.ToothNumber)))
            {
                continue;
            }

            var toothRes = PatientTooth.Create(demo.Patient.Id, demo.ToothNumber, demo.Condition, demo.Surface, demo.Notes);

            if (toothRes.IsError)
            {
                throw new Exception($"PatientTooth Seeding Failed: {toothRes.Errors}");
            }

            _context.PatientTeeth.Add(toothRes.Value);
            addedTeeth++;
        }

        if (addedTeeth > 0)
        {
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Creates a demo account when it is missing and otherwise repairs it.
    /// <para>
    /// The demo accounts exist so the quick-login buttons on the login page
    /// work on a fresh clone. A database that was seeded before the password
    /// policy changed still carries the old password, and a "create only when
    /// missing" rule would never repair it - the documented quick-login path
    /// would stay broken forever. The known demo password is therefore
    /// reconciled on every start, and a lockout is lifted, because the login
    /// errors are deliberately uniform and a locked-out demo account cannot be
    /// distinguished from a wrong password.
    /// </para>
    /// <para>
    /// This only ever runs for the Development demo data (the whole seeder is
    /// gated on the environment in Program.cs), never in production.
    /// </para>
    /// </summary>
    /// <returns>The persisted account, so the caller can link profiles to its real identifier.</returns>
    private async Task<AppUser> EnsureDemoAccountAsync(AppUser demoUser, string password, string role)
    {
        var user = await _userManager.FindByEmailAsync(demoUser.Email!);

        if (user is null)
        {
            user = demoUser;

            var created = await _userManager.CreateAsync(user, password);
            if (!created.Succeeded)
            {
                _logger.LogWarning(
                    "Could not create the demo account {Email}. Errors: {Errors}",
                    demoUser.Email,
                    created.Errors.Select(e => e.Description));

                return user;
            }
        }
        else
        {
            try
            {
                if (!await _userManager.CheckPasswordAsync(user, password))
                {
                    // The account exists with a different password (a database
                    // seeded before the password policy changed keeps the old
                    // one). The hash is replaced directly: the demo password is
                    // known, and AddIdentityCore registers no reset-token
                    // provider, so the reset-token route is not available.
                    var removed = await _userManager.RemovePasswordAsync(user);
                    if (!removed.Succeeded)
                    {
                        _logger.LogWarning(
                            "Could not clear the demo password for {Email}. Errors: {Errors}",
                            demoUser.Email,
                            removed.Errors.Select(e => e.Description));
                    }
                    else
                    {
                        var added = await _userManager.AddPasswordAsync(user, password);
                        if (!added.Succeeded)
                        {
                            _logger.LogWarning(
                                "Could not set the demo password for {Email}. Errors: {Errors}",
                                demoUser.Email,
                                added.Errors.Select(e => e.Description));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // A demo account that cannot be repaired must never stop the
                // application from starting.
                _logger.LogWarning(ex, "Could not reconcile the demo password for {Email}.", user.Email);
            }

            if (user.LockoutEnd is not null || user.AccessFailedCount > 0 || !user.EmailConfirmed)
            {
                user.LockoutEnd = null;
                user.AccessFailedCount = 0;
                user.EmailConfirmed = true;

                await _userManager.UpdateAsync(user);
            }
        }

        if (!await _userManager.IsInRoleAsync(user, role))
        {
            await _userManager.AddToRoleAsync(user, role);
        }

        return user;
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