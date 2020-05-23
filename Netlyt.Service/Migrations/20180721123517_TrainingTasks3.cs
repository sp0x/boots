using Microsoft.EntityFrameworkCore.Migrations;

namespace Netlyt.Service.Migrations
{
    public partial class TrainingTasks3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Scoring",
                table: "Models");

            migrationBuilder.AddColumn<string>(
                name: "Scoring",
                table: "TrainingTasks",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TargetId",
                table: "TrainingTasks",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingTasks_TargetId",
                table: "TrainingTasks",
                column: "TargetId");

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingTasks_ModelTargets_TargetId",
                table: "TrainingTasks",
                column: "TargetId",
                principalTable: "ModelTargets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrainingTasks_ModelTargets_TargetId",
                table: "TrainingTasks");

            migrationBuilder.DropIndex(
                name: "IX_TrainingTasks_TargetId",
                table: "TrainingTasks");

            migrationBuilder.DropColumn(
                name: "Scoring",
                table: "TrainingTasks");

            migrationBuilder.DropColumn(
                name: "TargetId",
                table: "TrainingTasks");

            migrationBuilder.AddColumn<string>(
                name: "Scoring",
                table: "Models",
                nullable: true);
        }
    }
}
