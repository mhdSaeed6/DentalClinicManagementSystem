using System.Data;

using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Domain.Appointments;
using DentalClinic.Domain.Doctors;
using DentalClinic.Domain.Identity;
using DentalClinic.Domain.Invoices;
using DentalClinic.Domain.Odontograms;
using DentalClinic.Domain.Patients;
using DentalClinic.Domain.Services;
using DentalClinic.Domain.TreatmentRecords;
using DentalClinic.Infrastructure.Identity;

using MediatR;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> contextOptions) : IdentityDbContext<AppUser>(contextOptions), IAppDbContext
{
    public DbSet<Doctor> Doctors => Set<Doctor>();

    public DbSet<Patient> Patients => Set<Patient>();

    public DbSet<Appointment> Appointments => Set<Appointment>();

    public DbSet<DentalService> DentalServices => Set<DentalService>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<TreatmentRecord> TreatmentRecords => Set<TreatmentRecord>();

    public DbSet<PatientTooth> PatientTeeth => Set<PatientTooth>();

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public async Task<IAppTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken)
    {
        var transaction = await Database.BeginTransactionAsync(isolationLevel, cancellationToken);
        return new EfAppTransaction(transaction);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
