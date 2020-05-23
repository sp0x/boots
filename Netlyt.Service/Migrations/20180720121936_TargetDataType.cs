using Microsoft.EntityFrameworkCore.Migrations;

namespace Netlyt.Service.Migrations
{
    public partial class TargetDataType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ClassifierType",
                table: "Models",
                newName: "Scoring");

            migrationBuilder.AddColumn<long>(
                name: "Months",
                table: "TimeConstraint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Years",
                table: "TimeConstraint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "ModelTargets",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Months",
                table: "TimeConstraint");

            migrationBuilder.DropColumn(
                name: "Years",
                table: "TimeConstraint");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "ModelTargets");

            migrationBuilder.RenameColumn(
                name: "Scoring",
                table: "Models",
                newName: "ClassifierType");
        }
    }
}
