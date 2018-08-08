using Microsoft.EntityFrameworkCore.Migrations;

namespace Netlyt.Service.Migrations
{
    public partial class TrainingTaskPerformanceDrop2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrainingTasks_ModelTrainingPerformance_PerformanceId",
                table: "TrainingTasks");

            migrationBuilder.AlterColumn<long>(
                name: "PerformanceId",
                table: "TrainingTasks",
                nullable: true,
                oldClrType: typeof(long));

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

            migrationBuilder.AlterColumn<long>(
                name: "PerformanceId",
                table: "TrainingTasks",
                nullable: false,
                oldClrType: typeof(long),
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingTasks_ModelTrainingPerformance_PerformanceId",
                table: "TrainingTasks",
                column: "PerformanceId",
                principalTable: "ModelTrainingPerformance",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
