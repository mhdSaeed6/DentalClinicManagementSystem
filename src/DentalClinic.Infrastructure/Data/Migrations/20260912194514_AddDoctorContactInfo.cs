using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalClinic.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorContactInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SocialMediaLink",
                table: "Doctors",
                newName: "ContactInfo_SocialMediaLink");

            migrationBuilder.RenameColumn(
                name: "SecondaryPhone",
                table: "Doctors",
                newName: "ContactInfo_SecondaryPhone");

            migrationBuilder.RenameColumn(
                name: "PrimaryPhone",
                table: "Doctors",
                newName: "ContactInfo_PrimaryPhone");

            migrationBuilder.RenameColumn(
                name: "HasWhatsAppOnPrimary",
                table: "Doctors",
                newName: "ContactInfo_HasWhatsAppOnPrimary");

            migrationBuilder.AlterColumn<string>(
                name: "ContactInfo_SocialMediaLink",
                table: "Doctors",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "ContactInfo_HasWhatsAppOnPrimary",
                table: "Doctors",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ContactInfo_SocialMediaLink",
                table: "Doctors",
                newName: "SocialMediaLink");

            migrationBuilder.RenameColumn(
                name: "ContactInfo_SecondaryPhone",
                table: "Doctors",
                newName: "SecondaryPhone");

            migrationBuilder.RenameColumn(
                name: "ContactInfo_PrimaryPhone",
                table: "Doctors",
                newName: "PrimaryPhone");

            migrationBuilder.RenameColumn(
                name: "ContactInfo_HasWhatsAppOnPrimary",
                table: "Doctors",
                newName: "HasWhatsAppOnPrimary");

            migrationBuilder.AlterColumn<string>(
                name: "SocialMediaLink",
                table: "Doctors",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "HasWhatsAppOnPrimary",
                table: "Doctors",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");
        }
    }
}
