using Microsoft.EntityFrameworkCore.Migrations;

namespace Netlyt.Service.Migrations
{
    public partial class CloudAuthEventUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "CloudAuthorizations",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CloudAuthorizations_UserId",
                table: "CloudAuthorizations",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CloudAuthorizations_AspNetUsers_UserId",
                table: "CloudAuthorizations",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CloudAuthorizations_AspNetUsers_UserId",
                table: "CloudAuthorizations");

            migrationBuilder.DropIndex(
                name: "IX_CloudAuthorizations_UserId",
                table: "CloudAuthorizations");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "CloudAuthorizations");
        }
    }
}
