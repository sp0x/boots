using Microsoft.EntityFrameworkCore.Migrations;

namespace Netlyt.Service.Migrations
{
    public partial class TargetConstraintDrop2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TargetConstraint_ModelTargets_ModelTargetId",
                table: "TargetConstraint");

            migrationBuilder.AlterColumn<long>(
                name: "ModelTargetId",
                table: "TargetConstraint",
                nullable: false,
                oldClrType: typeof(long),
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TargetConstraint_ModelTargets_ModelTargetId",
                table: "TargetConstraint",
                column: "ModelTargetId",
                principalTable: "ModelTargets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TargetConstraint_ModelTargets_ModelTargetId",
                table: "TargetConstraint");

            migrationBuilder.AlterColumn<long>(
                name: "ModelTargetId",
                table: "TargetConstraint",
                nullable: true,
                oldClrType: typeof(long));

            migrationBuilder.AddForeignKey(
                name: "FK_TargetConstraint_ModelTargets_ModelTargetId",
                table: "TargetConstraint",
                column: "ModelTargetId",
                principalTable: "ModelTargets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
