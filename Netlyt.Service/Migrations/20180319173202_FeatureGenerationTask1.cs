using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;


namespace Netlyt.Service.Migrations
{
    public partial class FeatureGenerationTask1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FieldExtras_Fields_FieldId",
                table: "FieldExtras");

            migrationBuilder.DropIndex(
                name: "IX_FieldExtras_FieldId",
                table: "FieldExtras");

            migrationBuilder.DropColumn(
                name: "FieldId",
                table: "FieldExtras");

            migrationBuilder.AddColumn<long>(
                name: "ExtrasId",
                table: "Fields",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FeatureGenerationTasks",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ModelId = table.Column<long>(nullable: false),
                    OrionTaskId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureGenerationTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeatureGenerationTasks_Models_ModelId",
                        column: x => x.ModelId,
                        principalTable: "Models",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Fields_ExtrasId",
                table: "Fields",
                column: "ExtrasId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeatureGenerationTasks_ModelId",
                table: "FeatureGenerationTasks",
                column: "ModelId");

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

            migrationBuilder.DropTable(
                name: "FeatureGenerationTasks");

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

            migrationBuilder.CreateIndex(
                name: "IX_FieldExtras_FieldId",
                table: "FieldExtras",
                column: "FieldId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FieldExtras_Fields_FieldId",
                table: "FieldExtras",
                column: "FieldId",
                principalTable: "Fields",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
