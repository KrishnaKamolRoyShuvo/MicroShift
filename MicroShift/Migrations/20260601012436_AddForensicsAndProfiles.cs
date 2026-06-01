using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MicroShift.Migrations
{
    /// <inheritdoc />
    public partial class AddForensicsAndProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EmployerDisputeUrl",
                table: "JobApplications",
                newName: "WorkerDisputeText");

            migrationBuilder.RenameColumn(
                name: "EmployerDisputeDate",
                table: "JobApplications",
                newName: "WorkerDisputeExifTime");

            migrationBuilder.AlterColumn<string>(
                name: "JobImageUrl",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisputeInitiator",
                table: "JobApplications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmployerDisputeExifTime",
                table: "JobApplications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmployerDisputeImageUrl",
                table: "JobApplications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmployerDisputeText",
                table: "JobApplications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkerDisputeImageUrl",
                table: "JobApplications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPermanentlyBanned",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ProfilePictureUrl",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SuspensionEndDate",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisputeInitiator",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "EmployerDisputeExifTime",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "EmployerDisputeImageUrl",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "EmployerDisputeText",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "WorkerDisputeImageUrl",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "IsPermanentlyBanned",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ProfilePictureUrl",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SuspensionEndDate",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "WorkerDisputeText",
                table: "JobApplications",
                newName: "EmployerDisputeUrl");

            migrationBuilder.RenameColumn(
                name: "WorkerDisputeExifTime",
                table: "JobApplications",
                newName: "EmployerDisputeDate");

            migrationBuilder.AlterColumn<string>(
                name: "JobImageUrl",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
