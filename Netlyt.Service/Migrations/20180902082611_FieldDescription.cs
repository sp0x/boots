using Microsoft.EntityFrameworkCore.Migrations;

namespace Netlyt.Service.Migrations
{
    public partial class FieldDescription : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DataType",
                table: "Fields",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionJson",
                table: "Fields",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataType",
                table: "Fields");

            migrationBuilder.DropColumn(
                name: "DescriptionJson",
                table: "Fields");
        }
    }
}
