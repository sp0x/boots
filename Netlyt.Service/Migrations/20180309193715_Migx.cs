using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Netlyt.Service.Migrations
{
    public partial class Migx : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Integrations_ApiKeys_APIKeyId",
                table: "Integrations");
            
            
            migrationBuilder.AlterColumn<long>(
                name: "IntegrationId",
                table: "ModelIntegration",
                nullable: false,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<long>(
                name: "APIKeyId",
                table: "Integrations",
                nullable: true,
                oldClrType: typeof(long));

            migrationBuilder.CreateIndex(
                name: "IX_ModelIntegration_IntegrationId",
                table: "ModelIntegration",
                column: "IntegrationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Integrations_ApiKeys_APIKeyId",
                table: "Integrations",
                column: "APIKeyId",
                principalTable: "ApiKeys",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ModelIntegration_Integrations_IntegrationId",
                table: "ModelIntegration",
                column: "IntegrationId",
                principalTable: "Integrations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Integrations_ApiKeys_APIKeyId",
                table: "Integrations");

            migrationBuilder.DropForeignKey(
                name: "FK_ModelIntegration_Integrations_IntegrationId",
                table: "ModelIntegration");

            migrationBuilder.DropIndex(
                name: "IX_ModelIntegration_IntegrationId",
                table: "ModelIntegration");

            migrationBuilder.AlterColumn<string>(
                name: "IntegrationId",
                table: "ModelIntegration",
                nullable: false,
                oldClrType: typeof(long));
            

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
    }
}
