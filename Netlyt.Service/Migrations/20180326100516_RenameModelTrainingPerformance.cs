using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Netlyt.Service.Migrations
{
    public partial class RenameModelTrainingPerformance : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ModelPerformance_Models_ModelId",
                table: "ModelPerformance");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ModelPerformance",
                table: "ModelPerformance");

            migrationBuilder.RenameTable(
                name: "ModelPerformance",
                newName: "ModelTrainingPerformance");

            migrationBuilder.RenameIndex(
                name: "IX_ModelPerformance_ModelId",
                table: "ModelTrainingPerformance",
                newName: "IX_ModelTrainingPerformance_ModelId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ModelTrainingPerformance",
                table: "ModelTrainingPerformance",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ModelTrainingPerformance_Models_ModelId",
                table: "ModelTrainingPerformance",
                column: "ModelId",
                principalTable: "Models",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ModelTrainingPerformance_Models_ModelId",
                table: "ModelTrainingPerformance");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ModelTrainingPerformance",
                table: "ModelTrainingPerformance");

            migrationBuilder.RenameTable(
                name: "ModelTrainingPerformance",
                newName: "ModelPerformance");

            migrationBuilder.RenameIndex(
                name: "IX_ModelTrainingPerformance_ModelId",
                table: "ModelPerformance",
                newName: "IX_ModelPerformance_ModelId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ModelPerformance",
                table: "ModelPerformance",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ModelPerformance_Models_ModelId",
                table: "ModelPerformance",
                column: "ModelId",
                principalTable: "Models",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
