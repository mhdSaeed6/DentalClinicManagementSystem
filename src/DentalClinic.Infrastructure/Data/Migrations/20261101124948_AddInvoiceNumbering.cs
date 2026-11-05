using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalClinic.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceNumbering : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_AppointmentId",
                table: "Invoices");

            migrationBuilder.AddColumn<string>(
                name: "InvoiceNumber",
                table: "Invoices",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "IssuedAtUtc",
                table: "Invoices",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InvoiceNumberCounters",
                columns: table => new
                {
                    Year = table.Column<int>(type: "int", nullable: false),
                    LastValue = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceNumberCounters", x => x.Year);
                    table.CheckConstraint("CK_InvoiceNumberCounters_LastValue_NonNegative", "[LastValue] >= 0");
                });

            // Pre-flight: the unique index below cannot be created while the
            // data already violates it. Fail with the offending appointment ids
            // rather than letting a migration silently delete money rows.
            migrationBuilder.Sql(
                """
                DECLARE @duplicateAppointments nvarchar(2048) = (
                    SELECT STRING_AGG(CONVERT(nvarchar(36), d.[AppointmentId]), N', ')
                    FROM (
                        SELECT [AppointmentId]
                        FROM [Invoices]
                        WHERE [AppointmentId] IS NOT NULL AND [IsDeleted] = 0
                        GROUP BY [AppointmentId]
                        HAVING COUNT(*) > 1
                    ) AS d
                );

                IF @duplicateAppointments IS NOT NULL
                BEGIN
                    DECLARE @message nvarchar(2048) =
                        N'Cannot enforce one invoice per appointment: these appointments already have more than one. '
                        + N'Resolve them (keep one, delete or soft-delete the rest), then re-run this migration. Appointments: '
                        + @duplicateAppointments;
                    THROW 50001, @message, 1;
                END;
                """);

            // Backfill: every already-issued invoice gets a number, ordered by
            // creation within its year, and its issuance instant. Drafts stay
            // unnumbered, exactly as the new invariant requires.
            //
            // The year comes from the UTC creation timestamp because the clinic
            // timezone is a deployment concern that a migration cannot see. The
            // only consequence is a possible off-by-one year for invoices issued
            // in the last hours of 31 December; ordering is still deterministic.
            migrationBuilder.Sql(
                """
                ;WITH [Numbered] AS (
                    SELECT
                        [Id],
                        YEAR([CreatedAtUtc]) AS [Year],
                        ROW_NUMBER() OVER (
                            PARTITION BY YEAR([CreatedAtUtc])
                            ORDER BY [CreatedAtUtc], [Id]) AS [Sequence]
                    FROM [Invoices]
                    WHERE [LifecycleStatus] = N'Issued'
                )
                UPDATE [i]
                SET
                    [i].[InvoiceNumber] = CONCAT(
                        N'INV-',
                        FORMAT([n].[Year], N'0000'),
                        N'-',
                        FORMAT([n].[Sequence], N'0000')),
                    [i].[IssuedAtUtc] = [i].[CreatedAtUtc]
                FROM [Invoices] AS [i]
                INNER JOIN [Numbered] AS [n] ON [n].[Id] = [i].[Id];
                """);

            // Continue each year's series after the highest backfilled number,
            // otherwise the next issuance would collide with a backfilled row.
            migrationBuilder.Sql(
                """
                INSERT INTO [InvoiceNumberCounters] ([Year], [LastValue])
                SELECT YEAR([CreatedAtUtc]), COUNT(*)
                FROM [Invoices]
                WHERE [LifecycleStatus] = N'Issued'
                GROUP BY YEAR([CreatedAtUtc]);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_AppointmentId",
                table: "Invoices",
                column: "AppointmentId",
                unique: true,
                filter: "[AppointmentId] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceNumber",
                table: "Invoices",
                column: "InvoiceNumber",
                unique: true,
                filter: "[InvoiceNumber] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceNumberCounters");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_AppointmentId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_InvoiceNumber",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "InvoiceNumber",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "IssuedAtUtc",
                table: "Invoices");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_AppointmentId",
                table: "Invoices",
                column: "AppointmentId");
        }
    }
}
