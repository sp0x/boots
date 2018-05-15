using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Netlyt.Service.Migrations
{
    public partial class FieldExtrasNoFieldId2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Fields_FieldExtras_ExtrasId",
                table: "Fields");

            migrationBuilder.AlterColumn<long>(
                name: "ExtrasId",
                table: "Fields",
                nullable: true,
                oldClrType: typeof(long));

            migrationBuilder.AddForeignKey(
                name: "FK_Fields_FieldExtras_ExtrasId",
                table: "Fields",
                column: "ExtrasId",
                principalTable: "FieldExtras",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Fields_FieldExtras_ExtrasId",
                table: "Fields");

            migrationBuilder.AlterColumn<long>(
                name: "ExtrasId",
                table: "Fields",
                nullable: false,
                oldClrType: typeof(long),
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Fields_FieldExtras_ExtrasId",
                table: "Fields",
                column: "ExtrasId",
                principalTable: "FieldExtras",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
