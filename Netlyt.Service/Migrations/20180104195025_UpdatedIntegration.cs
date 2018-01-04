using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Netlyt.Service.Migrations
{
    public partial class UpdatedIntegration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Integrations_ApiKeys_APIKeyId",
                table: "Integrations");

            migrationBuilder.AlterColumn<long>(
                name: "APIKeyId",
                table: "Integrations",
                nullable: false,
                oldClrType: typeof(long),
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Integrations_ApiKeys_APIKeyId",
                table: "Integrations",
                column: "APIKeyId",
                principalTable: "ApiKeys",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Integrations_ApiKeys_APIKeyId",
                table: "Integrations");

            migrationBuilder.AlterColumn<long>(
                name: "APIKeyId",
                table: "Integrations",
                nullable: true,
                oldClrType: typeof(long));

            migrationBuilder.AddForeignKey(
                name: "FK_Integrations_ApiKeys_APIKeyId",
                table: "Integrations",
                column: "APIKeyId",
                principalTable: "ApiKeys",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
