using Microsoft.EntityFrameworkCore.Migrations;

namespace Netlyt.Service.Migrations
{
    public partial class UserOrgId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Organizations_OrganizationId",
                table: "AspNetUsers");
            
            migrationBuilder.AlterColumn<long>(
                name: "OrganizationId",
                table: "AspNetUsers",
                nullable: false,
                oldClrType: typeof(long),
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Organizations_OrganizationId",
                table: "AspNetUsers",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Organizations_OrganizationId",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<long>(
                name: "OrganizationId",
                table: "AspNetUsers",
                nullable: true,
                oldClrType: typeof(long));

            migrationBuilder.InsertData(
                table: "ApiKeys",
                columns: new[] { "Id", "AppId", "AppSecret", "Endpoint", "Type" },
                values: new object[] { 1L, "2078f6d6dc54480cb5e29b7aedd3c95b", "lP56JSEYCN2dcOjfmUCt3/PK+uBY9aLV0buTjzdIjNE=", null, null });

            migrationBuilder.InsertData(
                table: "Organizations",
                columns: new[] { "Id", "ApiKeyId", "Name" },
                values: new object[] { 1L, 1L, "Netlyt" });

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Organizations_OrganizationId",
                table: "AspNetUsers",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
