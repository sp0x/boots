using Microsoft.EntityFrameworkCore.Migrations;

namespace Netlyt.Service.Migrations
{
    public partial class TrainingTasks7 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TypeInfo",
                table: "ModelTrainingPerformance");

            migrationBuilder.AddColumn<string>(
                name: "TypeInfo",
                table: "TrainingTasks",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TypeInfo",
                table: "TrainingTasks");

            migrationBuilder.AddColumn<string>(
                name: "TypeInfo",
                table: "ModelTrainingPerformance",
                nullable: true);
        }
    }
}
