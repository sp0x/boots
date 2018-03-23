using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Netlyt.Service.Migrations
{
    public partial class IntegrationHasTsAndIndexColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DataIndexColumn",
                table: "Integrations",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataTimestampColumn",
                table: "Integrations",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataIndexColumn",
                table: "Integrations");

            migrationBuilder.DropColumn(
                name: "DataTimestampColumn",
                table: "Integrations");
        }
    }
}
