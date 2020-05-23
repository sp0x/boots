using Microsoft.EntityFrameworkCore.Migrations;

namespace Netlyt.Service.Migrations
{
    public partial class Rrefresh5 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "OwnerId",
                table: "Permissions",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_OwnerId",
                table: "Permissions",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Permissions_Organizations_OwnerId",
                table: "Permissions",
                column: "OwnerId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Permissions_Organizations_OwnerId",
                table: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_OwnerId",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Permissions");
        }
    }
}
