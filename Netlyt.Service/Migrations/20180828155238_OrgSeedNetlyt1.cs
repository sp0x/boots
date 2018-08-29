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

            migrationBuilder.InsertData(
                table: "ApiKeys",
                columns: new[] { "Id", "AppId", "AppSecret", "Endpoint", "Type" },
                values: new object[] { 1L, "d1b309167fee46cfa7429cddafbcac73", "XAfuwRc6PuaBq8EDYMwmvv/fSHMAUJfTad7dssS30E4=", null, null });

            migrationBuilder.InsertData(
                table: "Organizations",
                columns: new[] { "Id", "ApiKeyId", "Name" },
                values: new object[] { 1L, 1L, "Netlyt" });

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

            migrationBuilder.DeleteData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: 1L);

            migrationBuilder.DeleteData(
                table: "ApiKeys",
                keyColumn: "Id",
                keyValue: 1L);

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
