using Microsoft.EntityFrameworkCore.Migrations;

namespace Netlyt.Service.Migrations
{
    public partial class OrgSeedNetlyt1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Organizations_ApiKeys_ApiKeyId",
                table: "Organizations");

            migrationBuilder.AlterColumn<long>(
                name: "ApiKeyId",
                table: "Organizations",
                nullable: false,
                oldClrType: typeof(long),
                oldNullable: true);
            
            migrationBuilder.AddForeignKey(
                name: "FK_Organizations_ApiKeys_ApiKeyId",
                table: "Organizations",
                column: "ApiKeyId",
                principalTable: "ApiKeys",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Organizations_ApiKeys_ApiKeyId",
                table: "Organizations");
             

            migrationBuilder.AlterColumn<long>(
                name: "ApiKeyId",
                table: "Organizations",
                nullable: true,
                oldClrType: typeof(long));

            migrationBuilder.AddForeignKey(
                name: "FK_Organizations_ApiKeys_ApiKeyId",
                table: "Organizations",
                column: "ApiKeyId",
                principalTable: "ApiKeys",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
