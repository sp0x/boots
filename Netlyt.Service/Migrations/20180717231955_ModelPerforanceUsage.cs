using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Netlyt.Service.Migrations
{
    public partial class ModelPerforanceUsage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastRequestIp",
                table: "ModelTrainingPerformance",
                type: "VARCHAR",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastRequestTs",
                table: "ModelTrainingPerformance",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "MonthlyUsage",
                table: "ModelTrainingPerformance",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WeeklyUsage",
                table: "ModelTrainingPerformance",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastRequestIp",
                table: "ModelTrainingPerformance");

            migrationBuilder.DropColumn(
                name: "LastRequestTs",
                table: "ModelTrainingPerformance");

            migrationBuilder.DropColumn(
                name: "MonthlyUsage",
                table: "ModelTrainingPerformance");

            migrationBuilder.DropColumn(
                name: "WeeklyUsage",
                table: "ModelTrainingPerformance");
        }
    }
}
