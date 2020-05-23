using Microsoft.EntityFrameworkCore.Migrations;

namespace Netlyt.Service.Migrations
{
    public partial class PermissionNavigationProps : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Permissions_Integrations_DataIntegrationId",
                table: "Permissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Permissions_Models_ModelId",
                table: "Permissions");

            migrationBuilder.AlterColumn<long>(
                name: "ModelId",
                table: "Permissions",
                nullable: false,
                oldClrType: typeof(long),
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "DataIntegrationId",
                table: "Permissions",
                nullable: false,
                oldClrType: typeof(long),
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Permissions_Integrations_DataIntegrationId",
                table: "Permissions",
                column: "DataIntegrationId",
                principalTable: "Integrations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Permissions_Models_ModelId",
                table: "Permissions",
                column: "ModelId",
                principalTable: "Models",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Permissions_Integrations_DataIntegrationId",
                table: "Permissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Permissions_Models_ModelId",
                table: "Permissions");

            migrationBuilder.AlterColumn<long>(
                name: "ModelId",
                table: "Permissions",
                nullable: true,
                oldClrType: typeof(long));

            migrationBuilder.AlterColumn<long>(
                name: "DataIntegrationId",
                table: "Permissions",
                nullable: true,
                oldClrType: typeof(long));

            migrationBuilder.AddForeignKey(
                name: "FK_Permissions_Integrations_DataIntegrationId",
                table: "Permissions",
                column: "DataIntegrationId",
                principalTable: "Integrations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Permissions_Models_ModelId",
                table: "Permissions",
                column: "ModelId",
                principalTable: "Models",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
