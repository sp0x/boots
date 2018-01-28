using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Netlyt.Service.Migrations
{
    public partial class DataIntegrationPublicKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ApiKeyId",
                table: "Organizations",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PublicKeyId",
                table: "Integrations",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_ApiKeyId",
                table: "Organizations",
                column: "ApiKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_Integrations_PublicKeyId",
                table: "Integrations",
                column: "PublicKeyId"); 

            migrationBuilder.AddForeignKey(
                name: "FK_Organizations_ApiKeys_ApiKeyId",
                table: "Organizations",
                column: "ApiKeyId",
                principalTable: "ApiKeys",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        { 

            migrationBuilder.DropForeignKey(
                name: "FK_Organizations_ApiKeys_ApiKeyId",
                table: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_Organizations_ApiKeyId",
                table: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_Integrations_PublicKeyId",
                table: "Integrations");

            migrationBuilder.DropColumn(
                name: "ApiKeyId",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "PublicKeyId",
                table: "Integrations");
        }
    }
}
