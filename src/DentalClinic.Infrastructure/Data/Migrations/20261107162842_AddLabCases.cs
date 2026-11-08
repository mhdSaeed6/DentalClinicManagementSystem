using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalClinic.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLabCases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LabCases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TreatmentRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ToothNumber = table.Column<int>(type: "int", nullable: true),
                    LabName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CaseType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SentAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpectedReturnAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReturnedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
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
                    table.PrimaryKey("PK_LabCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabCases_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LabCases_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LabCases_TreatmentRecords_TreatmentRecordId",
                        column: x => x.TreatmentRecordId,
                        principalTable: "TreatmentRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LabCases_DoctorId",
                table: "LabCases",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_LabCases_ExpectedReturnAtUtc",
                table: "LabCases",
                column: "ExpectedReturnAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_LabCases_PatientId",
                table: "LabCases",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_LabCases_PatientId_Status",
                table: "LabCases",
                columns: new[] { "PatientId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_LabCases_TreatmentRecordId",
                table: "LabCases",
                column: "TreatmentRecordId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LabCases");
        }
    }
}