using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Netlyt.Service.Migrations
{
    public partial class FieldExtrasNoFieldId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DonutScripts_Models_ModelId",
                table: "DonutScripts");

            migrationBuilder.DropForeignKey(
                name: "FK_Fields_FieldExtras_ExtrasId",
                table: "Fields");

            migrationBuilder.DropIndex(
                name: "IX_Fields_ExtrasId",
                table: "Fields");

            migrationBuilder.DropIndex(
                name: "IX_DonutScripts_ModelId",
                table: "DonutScripts");

            migrationBuilder.AddColumn<long>(
                name: "DonutScriptId",
                table: "Models",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AlterColumn<long>(
                name: "ExtrasId",
                table: "Fields",
                nullable: false,
                oldClrType: typeof(long),
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FieldId",
                table: "FieldExtras",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AlterColumn<long>(
                name: "ModelId",
                table: "DonutScripts",
                nullable: false,
                oldClrType: typeof(long),
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Models_DonutScriptId",
                table: "Models",
                column: "DonutScriptId");

            migrationBuilder.CreateIndex(
                name: "IX_Fields_ExtrasId",
                table: "Fields",
                column: "ExtrasId");

            migrationBuilder.CreateIndex(
                name: "IX_DonutScripts_ModelId",
                table: "DonutScripts",
                column: "ModelId");

            migrationBuilder.AddForeignKey(
                name: "FK_DonutScripts_Models_ModelId",
                table: "DonutScripts",
                column: "ModelId",
                principalTable: "Models",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Fields_FieldExtras_ExtrasId",
                table: "Fields",
                column: "ExtrasId",
                principalTable: "FieldExtras",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Models_DonutScripts_DonutScriptId",
                table: "Models",
                column: "DonutScriptId",
                principalTable: "DonutScripts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DonutScripts_Models_ModelId",
                table: "DonutScripts");

            migrationBuilder.DropForeignKey(
                name: "FK_Fields_FieldExtras_ExtrasId",
                table: "Fields");

            migrationBuilder.DropForeignKey(
                name: "FK_Models_DonutScripts_DonutScriptId",
                table: "Models");

            migrationBuilder.DropIndex(
                name: "IX_Models_DonutScriptId",
                table: "Models");

            migrationBuilder.DropIndex(
                name: "IX_Fields_ExtrasId",
                table: "Fields");

            migrationBuilder.DropIndex(
                name: "IX_DonutScripts_ModelId",
                table: "DonutScripts");

            migrationBuilder.DropColumn(
                name: "DonutScriptId",
                table: "Models");

            migrationBuilder.DropColumn(
                name: "FieldId",
                table: "FieldExtras");

            migrationBuilder.AlterColumn<long>(
                name: "ExtrasId",
                table: "Fields",
                nullable: true,
                oldClrType: typeof(long));

            migrationBuilder.AlterColumn<long>(
                name: "ModelId",
                table: "DonutScripts",
                nullable: true,
                oldClrType: typeof(long));

            migrationBuilder.CreateIndex(
                name: "IX_Fields_ExtrasId",
                table: "Fields",
                column: "ExtrasId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DonutScripts_ModelId",
                table: "DonutScripts",
                column: "ModelId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DonutScripts_Models_ModelId",
                table: "DonutScripts",
                column: "ModelId",
                principalTable: "Models",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Fields_FieldExtras_ExtrasId",
                table: "Fields",
                column: "ExtrasId",
                principalTable: "FieldExtras",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
