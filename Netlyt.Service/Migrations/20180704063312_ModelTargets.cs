using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Netlyt.Service.Migrations
{
    public partial class ModelTargets : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TargetAttribute",
                table: "Models");

            migrationBuilder.AddColumn<long>(
                name: "ModelTargetsId",
                table: "Fields",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ModelTargets",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ModelId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModelTargets_Models_ModelId",
                        column: x => x.ModelId,
                        principalTable: "Models",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TimeConstraint",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Hours = table.Column<int>(nullable: false),
                    Days = table.Column<int>(nullable: false),
                    Seconds = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeConstraint", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TargetConstraint",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Type = table.Column<int>(nullable: false),
                    Key = table.Column<string>(nullable: true),
                    AfterId = table.Column<long>(nullable: true),
                    BeforeId = table.Column<long>(nullable: true),
                    ModelTargetsId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TargetConstraint", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TargetConstraint_TimeConstraint_AfterId",
                        column: x => x.AfterId,
                        principalTable: "TimeConstraint",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TargetConstraint_TimeConstraint_BeforeId",
                        column: x => x.BeforeId,
                        principalTable: "TimeConstraint",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TargetConstraint_ModelTargets_ModelTargetsId",
                        column: x => x.ModelTargetsId,
                        principalTable: "ModelTargets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Fields_ModelTargetsId",
                table: "Fields",
                column: "ModelTargetsId");

            migrationBuilder.CreateIndex(
                name: "IX_ModelTargets_ModelId",
                table: "ModelTargets",
                column: "ModelId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TargetConstraint_AfterId",
                table: "TargetConstraint",
                column: "AfterId");

            migrationBuilder.CreateIndex(
                name: "IX_TargetConstraint_BeforeId",
                table: "TargetConstraint",
                column: "BeforeId");

            migrationBuilder.CreateIndex(
                name: "IX_TargetConstraint_ModelTargetsId",
                table: "TargetConstraint",
                column: "ModelTargetsId");

            migrationBuilder.AddForeignKey(
                name: "FK_Fields_ModelTargets_ModelTargetsId",
                table: "Fields",
                column: "ModelTargetsId",
                principalTable: "ModelTargets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Fields_ModelTargets_ModelTargetsId",
                table: "Fields");

            migrationBuilder.DropTable(
                name: "TargetConstraint");

            migrationBuilder.DropTable(
                name: "TimeConstraint");

            migrationBuilder.DropTable(
                name: "ModelTargets");

            migrationBuilder.DropIndex(
                name: "IX_Fields_ModelTargetsId",
                table: "Fields");

            migrationBuilder.DropColumn(
                name: "ModelTargetsId",
                table: "Fields");

            migrationBuilder.AddColumn<string>(
                name: "TargetAttribute",
                table: "Models",
                nullable: true);
        }
    }
}
