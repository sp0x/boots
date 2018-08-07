using Microsoft.EntityFrameworkCore.Migrations;

namespace Netlyt.Service.Migrations
{
    public partial class ModelPerformanceScoringAndTaskType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Scoring",
                table: "ModelTrainingPerformance",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaskType",
                table: "ModelTrainingPerformance",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Scoring",
                table: "ModelTrainingPerformance");

            migrationBuilder.DropColumn(
                name: "TaskType",
                table: "ModelTrainingPerformance");
        }
    }
}
