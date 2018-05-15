using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Netlyt.Service.Migrations
{
    public partial class ModelOptionalDonutScriptId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Models_DonutScripts_DonutScriptId",
                table: "Models");

            migrationBuilder.DropIndex(
                name: "IX_Models_DonutScriptId",
                table: "Models");

            migrationBuilder.DropIndex(
                name: "IX_DonutScripts_ModelId",
                table: "DonutScripts");

            migrationBuilder.AlterColumn<long>(
                name: "DonutScriptId",
                table: "Models",
                nullable: true,
                oldClrType: typeof(long));

            migrationBuilder.CreateIndex(
                name: "IX_DonutScripts_ModelId",
                table: "DonutScripts",
                column: "ModelId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DonutScripts_ModelId",
                table: "DonutScripts");

            migrationBuilder.AlterColumn<long>(
                name: "DonutScriptId",
                table: "Models",
                nullable: false,
                oldClrType: typeof(long),
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Models_DonutScriptId",
                table: "Models",
                column: "DonutScriptId");

            migrationBuilder.CreateIndex(
                name: "IX_DonutScripts_ModelId",
                table: "DonutScripts",
                column: "ModelId");

            migrationBuilder.AddForeignKey(
                name: "FK_Models_DonutScripts_DonutScriptId",
                table: "Models",
                column: "DonutScriptId",
                principalTable: "DonutScripts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
