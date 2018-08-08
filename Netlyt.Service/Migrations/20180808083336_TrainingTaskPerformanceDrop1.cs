using Microsoft.EntityFrameworkCore.Migrations;

namespace Netlyt.Service.Migrations
{
    public partial class TrainingTaskPerformanceDrop1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ModelTrainingPerformance_TaskId",
                table: "ModelTrainingPerformance");

            migrationBuilder.AddColumn<long>(
                name: "PerformanceId",
                table: "TrainingTasks",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingTasks_PerformanceId",
                table: "TrainingTasks",
                column: "PerformanceId");

            migrationBuilder.CreateIndex(
                name: "IX_ModelTrainingPerformance_TaskId",
                table: "ModelTrainingPerformance",
                column: "TaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingTasks_ModelTrainingPerformance_PerformanceId",
                table: "TrainingTasks",
                column: "PerformanceId",
                principalTable: "ModelTrainingPerformance",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrainingTasks_ModelTrainingPerformance_PerformanceId",
                table: "TrainingTasks");

            migrationBuilder.DropIndex(
                name: "IX_TrainingTasks_PerformanceId",
                table: "TrainingTasks");

            migrationBuilder.DropIndex(
                name: "IX_ModelTrainingPerformance_TaskId",
                table: "ModelTrainingPerformance");

            migrationBuilder.DropColumn(
                name: "PerformanceId",
                table: "TrainingTasks");

            migrationBuilder.CreateIndex(
                name: "IX_ModelTrainingPerformance_TaskId",
                table: "ModelTrainingPerformance",
                column: "TaskId",
                unique: true);
        }
    }
}
