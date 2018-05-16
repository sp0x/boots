using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Netlyt.Service.Migrations
{
    public partial class AggregateKeyOp : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AggregateKeys_DonutFunction_OperationId",
                table: "AggregateKeys");

            migrationBuilder.AlterColumn<long>(
                name: "OperationId",
                table: "AggregateKeys",
                nullable: false,
                oldClrType: typeof(long),
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AggregateKeys_DonutFunction_OperationId",
                table: "AggregateKeys",
                column: "OperationId",
                principalTable: "DonutFunction",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AggregateKeys_DonutFunction_OperationId",
                table: "AggregateKeys");

            migrationBuilder.AlterColumn<long>(
                name: "OperationId",
                table: "AggregateKeys",
                nullable: true,
                oldClrType: typeof(long));

            migrationBuilder.AddForeignKey(
                name: "FK_AggregateKeys_DonutFunction_OperationId",
                table: "AggregateKeys",
                column: "OperationId",
                principalTable: "DonutFunction",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
