using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalClinic.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientOdontogram : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PatientTeeth",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToothNumber = table.Column<int>(type: "int", nullable: false),
                    Condition = table.Column<int>(type: "int", nullable: false),
                    Surface = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_PatientTeeth", x => x.Id)
                        .Annotation("SqlServer:Clustered", true);
                    table.ForeignKey(
                        name: "FK_PatientTeeth_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PatientTeeth_PatientId",
                table: "PatientTeeth",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientTeeth_PatientId_ToothNumber",
                table: "PatientTeeth",
                columns: new[] { "PatientId", "ToothNumber" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PatientTeeth");
        }
    }
}
