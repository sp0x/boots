﻿using Microsoft.EntityFrameworkCore.Migrations;

namespace Netlyt.Service.Migrations
{
    public partial class TrainingTaskPerformanceDrop3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ModelTrainingPerformance_TrainingTasks_TaskId",
                table: "ModelTrainingPerformance");

            migrationBuilder.DropIndex(
                name: "IX_ModelTrainingPerformance_TaskId",
                table: "ModelTrainingPerformance");

            migrationBuilder.DropColumn(
                name: "TaskId",
                table: "ModelTrainingPerformance");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "TaskId",
                table: "ModelTrainingPerformance",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModelTrainingPerformance_TaskId",
                table: "ModelTrainingPerformance",
                column: "TaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_ModelTrainingPerformance_TrainingTasks_TaskId",
                table: "ModelTrainingPerformance",
                column: "TaskId",
                principalTable: "TrainingTasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
