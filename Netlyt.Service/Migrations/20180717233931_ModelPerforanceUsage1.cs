using Microsoft.EntityFrameworkCore.Migrations;

namespace Netlyt.Service.Migrations
{
    public partial class ModelPerforanceUsage1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastRequestIp",
                table: "ModelTrainingPerformance",
                newName: "LastRequestIP");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastRequestIP",
                table: "ModelTrainingPerformance",
                newName: "LastRequestIp");
        }
    }
}
