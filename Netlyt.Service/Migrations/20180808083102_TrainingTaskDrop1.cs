using Microsoft.EntityFrameworkCore.Migrations;

namespace Netlyt.Service.Migrations
{
    public partial class TrainingTaskDrop1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrainingTasks_ModelTargets_TargetId",
                table: "TrainingTasks");

            migrationBuilder.AlterColumn<long>(
                name: "TargetId",
                table: "TrainingTasks",
                nullable: false,
                oldClrType: typeof(long),
                oldNullable: true);

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

            migrationBuilder.AlterColumn<long>(
                name: "TargetId",
                table: "TrainingTasks",
                nullable: true,
                oldClrType: typeof(long));

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingTasks_ModelTargets_TargetId",
                table: "TrainingTasks",
                column: "TargetId",
                principalTable: "ModelTargets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
