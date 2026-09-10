using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Domain.Appointments;
using DentalClinic.Domain.Doctors;
using DentalClinic.Domain.Identity;
using DentalClinic.Domain.Invoices;
using DentalClinic.Domain.Patients;
using DentalClinic.Domain.Services;
using DentalClinic.Domain.TreatmentRecords;

using MediatR;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.Infrastructure.Data;

public class AppDbContext(DbContextOptions contextOptions) : IdentityDbContext<AppUser>(contextOptions), IAppDbContext
{
    public DbSet<Doctor> Doctors => Set<Doctor>();

    public DbSet<Patient> Patients => Set<Patient>();

    public DbSet<Appointment> Appointments => Set<Appointment>();

    public DbSet<DentalService> DentalServices => Set<DentalService>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Invoice> Invoices => Set<Invoice>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<TreatmentRecord> TreatmentRecords => Set<TreatmentRecord>();

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
