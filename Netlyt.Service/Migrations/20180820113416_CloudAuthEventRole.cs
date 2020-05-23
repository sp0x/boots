using Microsoft.EntityFrameworkCore.Migrations;

namespace Netlyt.Service.Migrations
{
    public partial class CloudAuthEventRole : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "CloudAuthorizations",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "CloudAuthorizations",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "CloudAuthorizations");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "CloudAuthorizations");
        }
    }
}
