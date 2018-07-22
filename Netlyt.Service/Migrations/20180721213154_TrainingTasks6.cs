using Microsoft.EntityFrameworkCore.Migrations;

namespace Netlyt.Service.Migrations
{
    public partial class TrainingTasks6 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TypeInfo",
                table: "ModelTrainingPerformance",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TypeInfo",
                table: "ModelTrainingPerformance");
        }
    }
}
