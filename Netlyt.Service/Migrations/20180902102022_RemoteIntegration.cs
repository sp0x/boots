using Microsoft.EntityFrameworkCore.Migrations;

namespace Netlyt.Service.Migrations
{
    public partial class RemoteIntegration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRemote",
                table: "Integrations",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "RemoteId",
                table: "Integrations",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRemote",
                table: "Integrations");

            migrationBuilder.DropColumn(
                name: "RemoteId",
                table: "Integrations");
        }
    }
}
