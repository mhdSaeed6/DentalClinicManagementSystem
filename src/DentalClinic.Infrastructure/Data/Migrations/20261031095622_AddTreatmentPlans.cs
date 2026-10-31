using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalClinic.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTreatmentPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedCost",
                table: "TreatmentRecords",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PlanId",
                table: "TreatmentRecords",
                type: "uniqueidentifier",
                nullable: true);

            // Status is added nullable on purpose: the not-null constraint is
            // applied only after the backfill below, so no temporary default
            // constraint is left behind for future scaffolds to trip on.
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "TreatmentRecords",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SessionNumber",
                table: "TreatmentRecords",
                type: "int",
                nullable: false,
                defaultValue: 1);

            // Backfill — plan §5.4: all existing rows are performed work in
            // session 1; the estimate mirrors the actual cost they were billed
            // at, so old data prints identically to new data. Explicit by
            // design so intent is reviewable and the equivalent of Down is
            // simply dropping the columns.
            migrationBuilder.Sql(
                "UPDATE TreatmentRecords SET [Status] = 2, SessionNumber = 1, EstimatedCost = [Cost];");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "TreatmentRecords",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "TreatmentPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_TreatmentPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TreatmentPlans_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TreatmentPlans_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentRecords_PatientId_Status",
                table: "TreatmentRecords",
                columns: new[] { "PatientId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentRecords_PlanId",
                table: "TreatmentRecords",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentPlans_DoctorId",
                table: "TreatmentPlans",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentPlans_PatientId",
                table: "TreatmentPlans",
                column: "PatientId");

            migrationBuilder.AddForeignKey(
                name: "FK_TreatmentRecords_TreatmentPlans_PlanId",
                table: "TreatmentRecords",
                column: "PlanId",
                principalTable: "TreatmentPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TreatmentRecords_TreatmentPlans_PlanId",
                table: "TreatmentRecords");

            migrationBuilder.DropTable(
                name: "TreatmentPlans");

            migrationBuilder.DropIndex(
                name: "IX_TreatmentRecords_PatientId_Status",
                table: "TreatmentRecords");

            migrationBuilder.DropIndex(
                name: "IX_TreatmentRecords_PlanId",
                table: "TreatmentRecords");

            migrationBuilder.DropColumn(
                name: "EstimatedCost",
                table: "TreatmentRecords");

            migrationBuilder.DropColumn(
                name: "PlanId",
                table: "TreatmentRecords");

            migrationBuilder.DropColumn(
                name: "SessionNumber",
                table: "TreatmentRecords");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "TreatmentRecords");
        }
    }
}
