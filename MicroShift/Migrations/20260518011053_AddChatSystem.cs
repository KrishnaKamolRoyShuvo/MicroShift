using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MicroShift.Migrations
{
    /// <inheritdoc />
    public partial class AddChatSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.AlterColumn<double>(
            //    name: "Longitude",
            //    table: "Jobs",
            //    type: "float",
            //    nullable: false,
            //    defaultValue: 0.0,
            //    oldClrType: typeof(double),
            //    oldType: "float",
            //    oldNullable: true);

            //migrationBuilder.AlterColumn<double>(
            //    name: "Latitude",
            //    table: "Jobs",
            //    type: "float",
            //    nullable: false,
            //    defaultValue: 0.0,
            //    oldClrType: typeof(double),
            //    oldType: "float",
            //    oldNullable: true);

            //migrationBuilder.AddColumn<string>(
            //    name: "AIModerationNote",
            //    table: "Jobs",
            //    type: "nvarchar(max)",
            //    nullable: true);

            //migrationBuilder.AddColumn<int>(
            //    name: "CategoryId",
            //    table: "Jobs",
            //    type: "int",
            //    nullable: false,
            //    defaultValue: 0);

            //migrationBuilder.AddColumn<double>(
            //    name: "FinalCommissionPercentage",
            //    table: "Jobs",
            //    type: "float",
            //    nullable: false,
            //    defaultValue: 0.0);

            //migrationBuilder.AddColumn<int>(
            //    name: "ImpressionCount",
            //    table: "Jobs",
            //    type: "int",
            //    nullable: false,
            //    defaultValue: 0);

            //migrationBuilder.AddColumn<bool>(
            //    name: "IsAIApproved",
            //    table: "Jobs",
            //    type: "bit",
            //    nullable: false,
            //    defaultValue: false);

            //migrationBuilder.AddColumn<bool>(
            //    name: "IsCommissionOverridden",
            //    table: "Jobs",
            //    type: "bit",
            //    nullable: false,
            //    defaultValue: false);

            //migrationBuilder.AddColumn<bool>(
            //    name: "IsEmergency",
            //    table: "Jobs",
            //    type: "bit",
            //    nullable: false,
            //    defaultValue: false);

            //migrationBuilder.AddColumn<string>(
            //    name: "JobImageUrl",
            //    table: "Jobs",
            //    type: "nvarchar(max)",
            //    nullable: true);

            //migrationBuilder.AddColumn<string>(
            //    name: "JobImageUrl2",
            //    table: "Jobs",
            //    type: "nvarchar(max)",
            //    nullable: true);

            //migrationBuilder.AddColumn<string>(
            //    name: "JobImageUrl3",
            //    table: "Jobs",
            //    type: "nvarchar(max)",
            //    nullable: true);

            //migrationBuilder.AddColumn<string>(
            //    name: "JobImageUrl4",
            //    table: "Jobs",
            //    type: "nvarchar(max)",
            //    nullable: true);

            //migrationBuilder.AddColumn<string>(
            //    name: "JobImageUrl5",
            //    table: "Jobs",
            //    type: "nvarchar(max)",
            //    nullable: true);

            //migrationBuilder.AddColumn<string>(
            //    name: "LocationDirections",
            //    table: "Jobs",
            //    type: "nvarchar(max)",
            //    nullable: true);

            //migrationBuilder.AddColumn<int>(
            //    name: "ViewCount",
            //    table: "Jobs",
            //    type: "int",
            //    nullable: false,
            //    defaultValue: 0);

            //migrationBuilder.AddColumn<double>(
            //    name: "AiFraudProbabilityScore",
            //    table: "JobApplications",
            //    type: "float",
            //    nullable: true);

            //migrationBuilder.AddColumn<DateTime>(
            //    name: "EmployerDisputeDate",
            //    table: "JobApplications",
            //    type: "datetime2",
            //    nullable: true);

            //migrationBuilder.AddColumn<string>(
            //    name: "EmployerDisputeUrl",
            //    table: "JobApplications",
            //    type: "nvarchar(max)",
            //    nullable: true);

            //migrationBuilder.AddColumn<bool>(
            //    name: "IsEmployerTimeFraudDetected",
            //    table: "JobApplications",
            //    type: "bit",
            //    nullable: false,
            //    defaultValue: false);

            //migrationBuilder.AddColumn<bool>(
            //    name: "IsWorkerTimeFraudDetected",
            //    table: "JobApplications",
            //    type: "bit",
            //    nullable: false,
            //    defaultValue: false);

            //migrationBuilder.AddColumn<DateTime>(
            //    name: "WorkerEvidenceDate",
            //    table: "JobApplications",
            //    type: "datetime2",
            //    nullable: true);

            //migrationBuilder.AddColumn<string>(
            //    name: "WorkerEvidenceUrl",
            //    table: "JobApplications",
            //    type: "nvarchar(max)",
            //    nullable: true);

            //migrationBuilder.AlterColumn<string>(
            //    name: "NationalIdNumber",
            //    table: "AspNetUsers",
            //    type: "nvarchar(max)",
            //    nullable: true,
            //    oldClrType: typeof(string),
            //    oldType: "nvarchar(max)");

            //migrationBuilder.AddColumn<bool>(
            //    name: "IsAccountFrozen",
            //    table: "AspNetUsers",
            //    type: "bit",
            //    nullable: false,
            //    defaultValue: false);

            //migrationBuilder.AddColumn<bool>(
            //    name: "IsShadowBanned",
            //    table: "AspNetUsers",
            //    type: "bit",
            //    nullable: false,
            //    defaultValue: false);

            //migrationBuilder.AddColumn<int>(
            //    name: "RecoveryFailedAttempts",
            //    table: "AspNetUsers",
            //    type: "int",
            //    nullable: false,
            //    defaultValue: 0);

            //migrationBuilder.AddColumn<DateTime>(
            //    name: "RecoveryLockoutEnd",
            //    table: "AspNetUsers",
            //    type: "datetime2",
            //    nullable: true);

            //migrationBuilder.CreateTable(
            //    name: "Categories",
            //    columns: table => new
            //    {
            //        Id = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
            //        Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
            //        CategoryCommissionPercentage = table.Column<double>(type: "float", nullable: true)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_Categories", x => x.Id);
            //    });

            //migrationBuilder.CreateTable(
            //    name: "Messages",
            //    columns: table => new
            //    {
            //        Id = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("SqlServer:Identity", "1, 1"),
            //        JobId = table.Column<int>(type: "int", nullable: false),
            //        SenderId = table.Column<string>(type: "nvarchar(450)", nullable: false),
            //        ReceiverId = table.Column<string>(type: "nvarchar(450)", nullable: false),
            //        Content = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
            //        SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
            //        IsFlaggedBySystem = table.Column<bool>(type: "bit", nullable: false),
            //        IsRead = table.Column<bool>(type: "bit", nullable: false)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_Messages", x => x.Id);
            //        table.ForeignKey(
            //            name: "FK_Messages_AspNetUsers_ReceiverId",
            //            column: x => x.ReceiverId,
            //            principalTable: "AspNetUsers",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Restrict);
            //        table.ForeignKey(
            //            name: "FK_Messages_AspNetUsers_SenderId",
            //            column: x => x.SenderId,
            //            principalTable: "AspNetUsers",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Restrict);
            //        table.ForeignKey(
            //            name: "FK_Messages_Jobs_JobId",
            //            column: x => x.JobId,
            //            principalTable: "Jobs",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //    });

            //migrationBuilder.CreateIndex(
            //    name: "IX_Jobs_CategoryId",
            //    table: "Jobs",
            //    column: "CategoryId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_Messages_JobId",
            //    table: "Messages",
            //    column: "JobId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_Messages_ReceiverId",
            //    table: "Messages",
            //    column: "ReceiverId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_Messages_SenderId",
            //    table: "Messages",
            //    column: "SenderId");

            //migrationBuilder.AddForeignKey(
            //    name: "FK_Jobs_Categories_CategoryId",
            //    table: "Jobs",
            //    column: "CategoryId",
            //    principalTable: "Categories",
            //    principalColumn: "Id",
            //    onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Categories_CategoryId",
                table: "Jobs");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_CategoryId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "AIModerationNote",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "FinalCommissionPercentage",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ImpressionCount",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "IsAIApproved",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "IsCommissionOverridden",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "IsEmergency",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "JobImageUrl",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "JobImageUrl2",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "JobImageUrl3",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "JobImageUrl4",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "JobImageUrl5",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "LocationDirections",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "AiFraudProbabilityScore",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "EmployerDisputeDate",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "EmployerDisputeUrl",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "IsEmployerTimeFraudDetected",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "IsWorkerTimeFraudDetected",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "WorkerEvidenceDate",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "WorkerEvidenceUrl",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "IsAccountFrozen",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsShadowBanned",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RecoveryFailedAttempts",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RecoveryLockoutEnd",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<double>(
                name: "Longitude",
                table: "Jobs",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<double>(
                name: "Latitude",
                table: "Jobs",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<string>(
                name: "NationalIdNumber",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
