using System.Globalization;

using DentalClinic.Api.IntegrationTests.Common;
using DentalClinic.Infrastructure.Data;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using Xunit;

namespace DentalClinic.Api.IntegrationTests;

/// <summary>
/// Covers the data side of <c>AddInvoiceNumbering</c>: the backfill of existing
/// invoices, the counter seeding that follows it, and the pre-flight guard that
/// refuses to add the one-invoice-per-appointment constraint to dirty data.
///
/// These need their own database at the migration *before* the numbering one,
/// which is why they do not reuse the fixture's shared database: that one is
/// already fully migrated by the other tests.
/// </summary>
public class InvoiceNumberingMigrationTests : IClassFixture<MsSqlMigrationFixture>
{
    /// <summary>The migration immediately preceding <c>AddInvoiceNumbering</c>.</summary>
    private const string PreviousMigration = "20261031095622_AddTreatmentPlans";

    private readonly MsSqlMigrationFixture _fixture;

    public InvoiceNumberingMigrationTests(MsSqlMigrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddInvoiceNumbering_ShouldBackfillIssuedInvoicesAndSeedCounters()
    {
        await using var context = await CreateDatabaseAtPreviousMigrationAsync();

        // Two issued invoices in 2026 (out of chronological insert order, to
        // prove the numbering follows creation time) and one in 2025. The draft
        // must stay unnumbered.
        await InsertInvoicesAsync(
            context,
            (Guid.NewGuid(), null, "Issued", "2026-02-01T09:00:00+00:00"),
            (Guid.NewGuid(), null, "Issued", "2025-12-31T10:00:00+00:00"),
            (Guid.NewGuid(), null, "Issued", "2026-01-01T08:00:00+00:00"),
            (Guid.NewGuid(), null, "Draft", "2026-03-01T08:00:00+00:00"));

        await context.Database.MigrateAsync();

        var invoices = await context.Invoices
            .AsNoTracking()
            .OrderBy(i => i.CreatedAtUtc)
            .Select(i => new { i.InvoiceNumber, i.LifecycleStatus, i.IssuedAtUtc })
            .ToListAsync();

        Assert.Equal("INV-2025-0001", invoices[0].InvoiceNumber);
        Assert.Equal("INV-2026-0001", invoices[1].InvoiceNumber);
        Assert.Equal("INV-2026-0002", invoices[2].InvoiceNumber);

        // The draft is not a bill, so it must remain unnumbered.
        Assert.Null(invoices[3].InvoiceNumber);

        // Every backfilled invoice records when it became a bill.
        Assert.All(invoices.Take(3), i => Assert.NotNull(i.IssuedAtUtc));

        // The counters continue the series instead of restarting it, otherwise
        // the very next issuance would collide with a backfilled row.
        var counters = await context.InvoiceNumberCounters
            .AsNoTracking()
            .OrderBy(c => c.Year)
            .ToListAsync();

        Assert.Equal(2, counters.Count);
        Assert.Equal(1, counters[0].LastValue); // 2025
        Assert.Equal(2, counters[1].LastValue); // 2026
    }

    [Fact]
    public async Task AddInvoiceNumbering_ShouldRefuseToRun_WhenAVisitHasTwoInvoices()
    {
        await using var context = await CreateDatabaseAtPreviousMigrationAsync();

        var duplicatedAppointment = Guid.NewGuid();
        await InsertInvoicesAsync(
            context,
            (Guid.NewGuid(), duplicatedAppointment, "Issued", "2026-01-01T08:00:00+00:00"),
            (Guid.NewGuid(), duplicatedAppointment, "Issued", "2026-01-02T08:00:00+00:00"));

        // Act - the unique index cannot be created over dirty data, and the
        // migration must say which appointment is at fault instead of silently
        // deleting one of the bills.
        var exception = await Assert.ThrowsAnyAsync<Exception>(
            () => context.Database.MigrateAsync());

        Assert.Contains(
            duplicatedAppointment.ToString(),
            exception.ToString(),
            StringComparison.OrdinalIgnoreCase);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task<AppDbContext> CreateDatabaseAtPreviousMigrationAsync()
    {
        var databaseName = "InvoiceNumbering_" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)[..12];
        var connectionString = _fixture.GetConnectionString(databaseName);

        var masterConnectionString = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master",
        }.ConnectionString;

        await using (var connection = new SqlConnection(masterConnectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE DATABASE [{databaseName}]";
            await command.ExecuteNonQueryAsync();
        }

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        var context = new AppDbContext(options);
        await context.GetService<IMigrator>().MigrateAsync(PreviousMigration);

        return context;
    }

    /// <summary>
    /// Inserts invoices straight into the pre-numbering schema. EF cannot be used
    /// here because its model already knows about the columns this migration adds.
    /// </summary>
    private static async Task InsertInvoicesAsync(
        AppDbContext context,
        params (Guid Id, Guid? AppointmentId, string LifecycleStatus, string CreatedAtUtc)[] invoices)
    {
        // The patient foreign key is irrelevant to numbering; skipping it keeps
        // the fixture free of unrelated patient columns.
        await context.Database.ExecuteSqlRawAsync("ALTER TABLE [Invoices] NOCHECK CONSTRAINT ALL");

        foreach (var invoice in invoices)
        {
            await context.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO [Invoices]
                    ([Id], [PatientId], [AppointmentId], [TotalAmount], [PaidAmount], [Status],
                     [LifecycleStatus], [HasManualAdjustments], [CreatedAtUtc], [IsDeleted])
                VALUES
                    ({0}, NEWID(), {1}, 100.00, 0.00, N'Unpaid',
                     {2}, 0, {3}, 0)
                """,
                invoice.Id,
                invoice.AppointmentId,
                invoice.LifecycleStatus,
                DateTimeOffset.Parse(invoice.CreatedAtUtc, CultureInfo.InvariantCulture));
        }

        await context.Database.ExecuteSqlRawAsync("ALTER TABLE [Invoices] CHECK CONSTRAINT ALL");
    }
}