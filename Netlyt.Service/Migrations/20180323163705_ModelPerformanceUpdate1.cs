using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Netlyt.Service.Migrations
{
    public partial class ModelPerformanceUpdate1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReportUrl",
                table: "ModelPerformance",
                type: "VARCHAR",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TestResultsUrl",
                table: "ModelPerformance",
                type: "VARCHAR",
                maxLength: 255,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReportUrl",
                table: "ModelPerformance");

            migrationBuilder.DropColumn(
                name: "TestResultsUrl",
                table: "ModelPerformance");
        }
    }
}
