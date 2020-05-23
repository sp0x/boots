using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Netlyt.Service.Migrations
{
    public partial class ModelGrouping2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrainingScript",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    DonutScript = table.Column<string>(nullable: true),
                    PythonScript = table.Column<string>(nullable: true),
                    TrainingTaskId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingScript", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingScript_TrainingTasks_TrainingTaskId",
                        column: x => x.TrainingTaskId,
                        principalTable: "TrainingTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingScript_TrainingTaskId",
                table: "TrainingScript",
                column: "TrainingTaskId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrainingScript");
        }
    }
}
