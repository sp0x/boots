using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Netlyt.Service.Migrations
{
    public partial class AuthUpdate1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ModelRule",
                table: "ModelRule");

            migrationBuilder.DropIndex(
                name: "IX_ModelRule_ModelId",
                table: "ModelRule");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ModelIntegration",
                table: "ModelIntegration");

            migrationBuilder.DropIndex(
                name: "IX_ModelIntegration_ModelId",
                table: "ModelIntegration");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "ModelRule");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "ModelIntegration");

            migrationBuilder.AlterColumn<string>(
                name: "IntegrationId",
                table: "ModelIntegration",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AppId",
                table: "ApiKeys",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ModelRule",
                table: "ModelRule",
                columns: new[] { "ModelId", "RuleId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ModelIntegration",
                table: "ModelIntegration",
                columns: new[] { "ModelId", "IntegrationId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ModelRule",
                table: "ModelRule");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ModelIntegration",
                table: "ModelIntegration");

            migrationBuilder.AddColumn<long>(
                name: "Id",
                table: "ModelRule",
                nullable: false,
                defaultValue: 0L)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AlterColumn<string>(
                name: "IntegrationId",
                table: "ModelIntegration",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AddColumn<long>(
                name: "Id",
                table: "ModelIntegration",
                nullable: false,
                defaultValue: 0L)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn);

            migrationBuilder.AlterColumn<string>(
                name: "AppId",
                table: "ApiKeys",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AddPrimaryKey(
                name: "PK_ModelRule",
                table: "ModelRule",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ModelIntegration",
                table: "ModelIntegration",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ModelRule_ModelId",
                table: "ModelRule",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_ModelIntegration_ModelId",
                table: "ModelIntegration",
                column: "ModelId");
        }
    }
}
