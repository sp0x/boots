using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Netlyt.Service.Migrations
{
    public partial class Rrefresh4 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    ShareWithId = table.Column<long>(nullable: true),
                    CanRead = table.Column<bool>(nullable: false),
                    CanModify = table.Column<bool>(nullable: false),
                    DataIntegrationId = table.Column<long>(nullable: true),
                    ModelId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Permissions_Integrations_DataIntegrationId",
                        column: x => x.DataIntegrationId,
                        principalTable: "Integrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Permissions_Models_ModelId",
                        column: x => x.ModelId,
                        principalTable: "Models",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Permissions_Organizations_ShareWithId",
                        column: x => x.ShareWithId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_DataIntegrationId",
                table: "Permissions",
                column: "DataIntegrationId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_ModelId",
                table: "Permissions",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_ShareWithId",
                table: "Permissions",
                column: "ShareWithId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Permissions");
        }
    }
}
