using Microsoft.EntityFrameworkCore.Migrations;

namespace Netlyt.Service.Migrations
{
    public partial class RemoteModels : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRemote",
                table: "Models",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "RemoteId",
                table: "Models",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRemote",
                table: "Models");

            migrationBuilder.DropColumn(
                name: "RemoteId",
                table: "Models");
        }
    }
}
