using System.Data;

using DentalClinic.Domain.Appointments;
using DentalClinic.Domain.Clinics;
using DentalClinic.Domain.Doctors;
using DentalClinic.Domain.Identity;
using DentalClinic.Domain.Invoices;
using DentalClinic.Domain.Odontograms;
using DentalClinic.Domain.Patients;
using DentalClinic.Domain.Services;
using DentalClinic.Domain.TreatmentPlans;
using DentalClinic.Domain.TreatmentRecords;

using Microsoft.EntityFrameworkCore;

namespace DentalClinic.Application.Common.Interfaces;

public interface IAppDbContext
{
    public DbSet<Doctor> Doctors { get; }
    public DbSet<Patient> Patients { get; }
    public DbSet<Appointment> Appointments { get; }
    public DbSet<DentalService> DentalServices { get; }
    public DbSet<RefreshToken> RefreshTokens { get; }
    public DbSet<Invoice> Invoices { get; }
    public DbSet<InvoiceLine> InvoiceLines { get; }
    public DbSet<Payment> Payments { get; }
    public DbSet<InvoiceNumberCounter> InvoiceNumberCounters { get; }
    public DbSet<TreatmentRecord> TreatmentRecords { get; }
    public DbSet<TreatmentPlan> TreatmentPlans { get; }
    public DbSet<PatientTooth> PatientTeeth { get; }
    public DbSet<ClinicSetting> ClinicSettings { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    Task<IAppTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken);

    /// <summary>
    /// True while an explicit transaction is open. Numbering allocation uses
    /// this to refuse to run without one: outside a transaction the row lock is
    /// released immediately and two concurrent issues would read the same value.
    /// </summary>
    bool HasActiveTransaction { get; }
}