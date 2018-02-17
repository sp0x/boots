using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Netlyt.Service.Migrations
{
    public partial class AddedIntegrationToFieldDefinition : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Fields_Integrations_DataIntegrationId",
                table: "Fields");

            migrationBuilder.DropIndex(
                name: "IX_Fields_DataIntegrationId",
                table: "Fields");

            migrationBuilder.DropColumn(
                name: "DataIntegrationId",
                table: "Fields");

            migrationBuilder.AddColumn<long>(
                name: "IntegrationId",
                table: "Fields",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_Fields_IntegrationId",
                table: "Fields",
                column: "IntegrationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Fields_Integrations_IntegrationId",
                table: "Fields",
                column: "IntegrationId",
                principalTable: "Integrations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Fields_Integrations_IntegrationId",
                table: "Fields");

            migrationBuilder.DropIndex(
                name: "IX_Fields_IntegrationId",
                table: "Fields");

            migrationBuilder.DropColumn(
                name: "IntegrationId",
                table: "Fields");

            migrationBuilder.AddColumn<long>(
                name: "DataIntegrationId",
                table: "Fields",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Fields_DataIntegrationId",
                table: "Fields",
                column: "DataIntegrationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Fields_Integrations_DataIntegrationId",
                table: "Fields",
                column: "DataIntegrationId",
                principalTable: "Integrations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
