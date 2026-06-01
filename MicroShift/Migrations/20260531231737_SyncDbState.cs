using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MicroShift.Migrations
{
    /// <inheritdoc />
    public partial class SyncDbState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Reviews");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "AdminDisputeNote",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ExifCaptureTime",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ExifLatitude",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ExifLongitude",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "FaultAssignedTo",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "JobType",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "Shift",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "AverageRating",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TotalReviews",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "WalletBalance",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Categories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
