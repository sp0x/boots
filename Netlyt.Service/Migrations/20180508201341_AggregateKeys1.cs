using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Netlyt.Service.Migrations
{
    public partial class AggregateKeys1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DonutFunction",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Body = table.Column<string>(nullable: true),
                    GroupValue = table.Column<string>(nullable: true),
                    IsAggregate = table.Column<bool>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    Projection = table.Column<string>(nullable: true),
                    Type = table.Column<int>(nullable: false),
                    Parameters = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonutFunction", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AggregateKeys",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Arguments = table.Column<string>(nullable: true),
                    DataIntegrationId = table.Column<long>(nullable: true),
                    Name = table.Column<string>(nullable: true),
                    OperationId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AggregateKeys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AggregateKeys_Integrations_DataIntegrationId",
                        column: x => x.DataIntegrationId,
                        principalTable: "Integrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AggregateKeys_DonutFunction_OperationId",
                        column: x => x.OperationId,
                        principalTable: "DonutFunction",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AggregateKeys_DataIntegrationId",
                table: "AggregateKeys",
                column: "DataIntegrationId");

            migrationBuilder.CreateIndex(
                name: "IX_AggregateKeys_OperationId",
                table: "AggregateKeys",
                column: "OperationId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AggregateKeys");

            migrationBuilder.DropTable(
                name: "DonutFunction");
        }
    }
}
