using Microsoft.EntityFrameworkCore.Migrations;

namespace Netlyt.Service.Migrations
{
    public partial class FieldExtraExtras : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FieldExtra_FieldExtras_FieldExtrasId",
                table: "FieldExtra");

            migrationBuilder.AlterColumn<long>(
                name: "FieldExtrasId",
                table: "FieldExtra",
                nullable: false,
                oldClrType: typeof(long),
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FieldExtra_FieldExtras_FieldExtrasId",
                table: "FieldExtra",
                column: "FieldExtrasId",
                principalTable: "FieldExtras",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FieldExtra_FieldExtras_FieldExtrasId",
                table: "FieldExtra");

            migrationBuilder.AlterColumn<long>(
                name: "FieldExtrasId",
                table: "FieldExtra",
                nullable: true,
                oldClrType: typeof(long));

            migrationBuilder.AddForeignKey(
                name: "FK_FieldExtra_FieldExtras_FieldExtrasId",
                table: "FieldExtra",
                column: "FieldExtrasId",
                principalTable: "FieldExtras",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
