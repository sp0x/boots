using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Netlyt.Service.Migrations
{
    public partial class OptionalDataIntegrationPublicKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        { 

            migrationBuilder.AlterColumn<long>(
                name: "PublicKeyId",
                table: "Integrations",
                nullable: true,
                oldClrType: typeof(long));
             
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        { 

            migrationBuilder.AlterColumn<long>(
                name: "PublicKeyId",
                table: "Integrations",
                nullable: false,
                oldClrType: typeof(long),
                oldNullable: true); 
        }
    }
}
