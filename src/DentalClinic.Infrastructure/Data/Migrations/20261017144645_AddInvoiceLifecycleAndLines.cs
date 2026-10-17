using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalClinic.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceLifecycleAndLines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LifecycleStatus",
                table: "Invoices",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Issued");

            // Doctors.UserId was already present in databases created by the old
            // EnsureCreated bootstrap but was never captured in a migration.
            // Guarded so this migration succeeds on existing databases and still
            // provisions the column on fresh ones.
            //
            // The ALTER and the CREATE INDEX must be separate batches: SQL Server
            // resolves names at batch compile time, so a filtered index created in
            // the same batch as the ALTER cannot see the new column.
            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[Doctors]') AND name = N'UserId')
BEGIN
    ALTER TABLE [Doctors] ADD [UserId] nvarchar(450) NULL;
END");

            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Doctors_UserId' AND object_id = OBJECT_ID(N'[Doctors]'))
BEGIN
    CREATE UNIQUE INDEX [IX_Doctors_UserId] ON [Doctors]([UserId]) WHERE [UserId] IS NOT NULL;
END");

            migrationBuilder.CreateTable(
                name: "InvoiceLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceLines", x => x.Id)
                        .Annotation("SqlServer:Clustered", true);
                    table.ForeignKey(
                        name: "FK_InvoiceLines_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_LifecycleStatus",
                table: "Invoices",
                column: "LifecycleStatus");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_InvoiceId",
                table: "InvoiceLines",
                column: "InvoiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceLines");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_LifecycleStatus",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "LifecycleStatus",
                table: "Invoices");

            // Doctors.UserId is intentionally left in place on Down: it predates
            // this migration and may be required by existing user/doctor links.
        }
    }
}
