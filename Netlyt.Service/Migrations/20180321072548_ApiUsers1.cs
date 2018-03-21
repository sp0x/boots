using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Netlyt.Service.Migrations
{
    public partial class ApiUsers1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApiUser_ApiKeys_ApiId",
                table: "ApiUser");

            migrationBuilder.DropForeignKey(
                name: "FK_ApiUser_AspNetUsers_UserId",
                table: "ApiUser");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ApiUser",
                table: "ApiUser");

            migrationBuilder.RenameTable(
                name: "ApiUser",
                newName: "ApiUsers");

            migrationBuilder.RenameIndex(
                name: "IX_ApiUser_UserId",
                table: "ApiUsers",
                newName: "IX_ApiUsers_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ApiUsers",
                table: "ApiUsers",
                columns: new[] { "ApiId", "UserId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ApiUsers_ApiKeys_ApiId",
                table: "ApiUsers",
                column: "ApiId",
                principalTable: "ApiKeys",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ApiUsers_AspNetUsers_UserId",
                table: "ApiUsers",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApiUsers_ApiKeys_ApiId",
                table: "ApiUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_ApiUsers_AspNetUsers_UserId",
                table: "ApiUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ApiUsers",
                table: "ApiUsers");

            migrationBuilder.RenameTable(
                name: "ApiUsers",
                newName: "ApiUser");

            migrationBuilder.RenameIndex(
                name: "IX_ApiUsers_UserId",
                table: "ApiUser",
                newName: "IX_ApiUser_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ApiUser",
                table: "ApiUser",
                columns: new[] { "ApiId", "UserId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ApiUser_ApiKeys_ApiId",
                table: "ApiUser",
                column: "ApiId",
                principalTable: "ApiKeys",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ApiUser_AspNetUsers_UserId",
                table: "ApiUser",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
