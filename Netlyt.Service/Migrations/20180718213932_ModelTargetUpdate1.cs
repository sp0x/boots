using Microsoft.EntityFrameworkCore.Migrations;

namespace Netlyt.Service.Migrations
{
    public partial class ModelTargetUpdate1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Fields_ModelTargets_ModelTargetsId",
                table: "Fields");

            migrationBuilder.DropForeignKey(
                name: "FK_TargetConstraint_ModelTargets_ModelTargetsId",
                table: "TargetConstraint");

            migrationBuilder.DropIndex(
                name: "IX_ModelTargets_ModelId",
                table: "ModelTargets");

            migrationBuilder.DropIndex(
                name: "IX_Fields_ModelTargetsId",
                table: "Fields");

            migrationBuilder.DropColumn(
                name: "ModelTargetsId",
                table: "Fields");

            migrationBuilder.RenameColumn(
                name: "ModelTargetsId",
                table: "TargetConstraint",
                newName: "ModelTargetId");

            migrationBuilder.RenameIndex(
                name: "IX_TargetConstraint_ModelTargetsId",
                table: "TargetConstraint",
                newName: "IX_TargetConstraint_ModelTargetId");

            migrationBuilder.AddColumn<long>(
                name: "ColumnId",
                table: "ModelTargets",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModelTargets_ColumnId",
                table: "ModelTargets",
                column: "ColumnId");

            migrationBuilder.CreateIndex(
                name: "IX_ModelTargets_ModelId",
                table: "ModelTargets",
                column: "ModelId");

            migrationBuilder.AddForeignKey(
                name: "FK_ModelTargets_Fields_ColumnId",
                table: "ModelTargets",
                column: "ColumnId",
                principalTable: "Fields",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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
                name: "FK_ModelTargets_Fields_ColumnId",
                table: "ModelTargets");

            migrationBuilder.DropForeignKey(
                name: "FK_TargetConstraint_ModelTargets_ModelTargetId",
                table: "TargetConstraint");

            migrationBuilder.DropIndex(
                name: "IX_ModelTargets_ColumnId",
                table: "ModelTargets");

            migrationBuilder.DropIndex(
                name: "IX_ModelTargets_ModelId",
                table: "ModelTargets");

            migrationBuilder.DropColumn(
                name: "ColumnId",
                table: "ModelTargets");

            migrationBuilder.RenameColumn(
                name: "ModelTargetId",
                table: "TargetConstraint",
                newName: "ModelTargetsId");

            migrationBuilder.RenameIndex(
                name: "IX_TargetConstraint_ModelTargetId",
                table: "TargetConstraint",
                newName: "IX_TargetConstraint_ModelTargetsId");

            migrationBuilder.AddColumn<long>(
                name: "ModelTargetsId",
                table: "Fields",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModelTargets_ModelId",
                table: "ModelTargets",
                column: "ModelId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Fields_ModelTargetsId",
                table: "Fields",
                column: "ModelTargetsId");

            migrationBuilder.AddForeignKey(
                name: "FK_Fields_ModelTargets_ModelTargetsId",
                table: "Fields",
                column: "ModelTargetsId",
                principalTable: "ModelTargets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TargetConstraint_ModelTargets_ModelTargetsId",
                table: "TargetConstraint",
                column: "ModelTargetsId",
                principalTable: "ModelTargets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
