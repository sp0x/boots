using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Netlyt.Service.Migrations
{
    public partial class AddedIntegrationToFieldDefinition1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Fields_FieldExtras_ExtrasId",
                table: "Fields");

            migrationBuilder.DropIndex(
                name: "IX_Fields_ExtrasId",
                table: "Fields");

            migrationBuilder.DropColumn(
                name: "ExtrasId",
                table: "Fields");

            migrationBuilder.AddColumn<long>(
                name: "FieldId",
                table: "FieldExtras",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FieldId",
                table: "FieldExtra",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FieldExtras_FieldId",
                table: "FieldExtras",
                column: "FieldId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FieldExtra_FieldId",
                table: "FieldExtra",
                column: "FieldId");

            migrationBuilder.AddForeignKey(
                name: "FK_FieldExtra_Fields_FieldId",
                table: "FieldExtra",
                column: "FieldId",
                principalTable: "Fields",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FieldExtras_Fields_FieldId",
                table: "FieldExtras",
                column: "FieldId",
                principalTable: "Fields",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FieldExtra_Fields_FieldId",
                table: "FieldExtra");

            migrationBuilder.DropForeignKey(
                name: "FK_FieldExtras_Fields_FieldId",
                table: "FieldExtras");

            migrationBuilder.DropIndex(
                name: "IX_FieldExtras_FieldId",
                table: "FieldExtras");

            migrationBuilder.DropIndex(
                name: "IX_FieldExtra_FieldId",
                table: "FieldExtra");

            migrationBuilder.DropColumn(
                name: "FieldId",
                table: "FieldExtras");

            migrationBuilder.DropColumn(
                name: "FieldId",
                table: "FieldExtra");

            migrationBuilder.AddColumn<long>(
                name: "ExtrasId",
                table: "Fields",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Fields_ExtrasId",
                table: "Fields",
                column: "ExtrasId");

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
