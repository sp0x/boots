using Microsoft.EntityFrameworkCore.Migrations;

namespace Netlyt.Service.Migrations
{
    public partial class OrgSeedNetlyt3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ApiKeys",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "AppId", "AppSecret" },
                values: new object[] { "1c19117c9c624eb8802882bf3555c734", "cUYp9gagutZBPO3IJebnCh/XXJu9OWFZx3Jc590IrzA=" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ApiKeys",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "AppId", "AppSecret" },
                values: new object[] { "2a5c672cb6004ca296efee95ce94b46e", "D4uRBIvGFuXmhF/QwMUK6YpcUMiyzo5lFZ7WP46573c=" });
        }
    }
}
