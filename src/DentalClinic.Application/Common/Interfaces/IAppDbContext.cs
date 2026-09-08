using DentalClinic.Domain.Appointments;
using DentalClinic.Domain.Doctors;
using DentalClinic.Domain.Identity;
using DentalClinic.Domain.Invoices;
using DentalClinic.Domain.Patients;
using DentalClinic.Domain.Services;
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
    public DbSet<Payment> Payments { get; }
    public DbSet<TreatmentRecord> TreatmentRecords { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}