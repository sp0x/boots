using Microsoft.EntityFrameworkCore.Migrations;

namespace Netlyt.Service.Migrations
{
    public partial class ModelKeys : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "APIKeyId",
                table: "Models",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "PublicKeyId",
                table: "Models",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Models_APIKeyId",
                table: "Models",
                column: "APIKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_Models_PublicKeyId",
                table: "Models",
                column: "PublicKeyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Models_ApiKeys_APIKeyId",
                table: "Models",
                column: "APIKeyId",
                principalTable: "ApiKeys",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Models_ApiKeys_PublicKeyId",
                table: "Models",
                column: "PublicKeyId",
                principalTable: "ApiKeys",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Models_ApiKeys_APIKeyId",
                table: "Models");

            migrationBuilder.DropForeignKey(
                name: "FK_Models_ApiKeys_PublicKeyId",
                table: "Models");

            migrationBuilder.DropIndex(
                name: "IX_Models_APIKeyId",
                table: "Models");

            migrationBuilder.DropIndex(
                name: "IX_Models_PublicKeyId",
                table: "Models");

            migrationBuilder.DropColumn(
                name: "APIKeyId",
                table: "Models");

            migrationBuilder.DropColumn(
                name: "PublicKeyId",
                table: "Models");
        }
    }
}
