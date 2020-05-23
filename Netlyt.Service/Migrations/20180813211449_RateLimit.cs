using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Netlyt.Service.Migrations
{
    public partial class RateLimit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "RateLimitId",
                table: "AspNetUsers",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "Rates",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Daily = table.Column<int>(nullable: false),
                    Monthly = table.Column<int>(nullable: false),
                    Weekly = table.Column<int>(nullable: false),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_RateLimitId",
                table: "AspNetUsers",
                column: "RateLimitId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Rates_RateLimitId",
                table: "AspNetUsers",
                column: "RateLimitId",
                principalTable: "Rates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Rates_RateLimitId",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Rates");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_RateLimitId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RateLimitId",
                table: "AspNetUsers");
        }
    }
}
