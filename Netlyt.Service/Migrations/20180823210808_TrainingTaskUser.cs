using Microsoft.EntityFrameworkCore.Migrations;

namespace Netlyt.Service.Migrations
{
    public partial class TrainingTaskUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "TrainingTasks",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingTasks_UserId",
                table: "TrainingTasks",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingTasks_AspNetUsers_UserId",
                table: "TrainingTasks",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrainingTasks_AspNetUsers_UserId",
                table: "TrainingTasks");

            migrationBuilder.DropIndex(
                name: "IX_TrainingTasks_UserId",
                table: "TrainingTasks");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "TrainingTasks");
        }
    }
}
