using Microsoft.EntityFrameworkCore.Migrations;

namespace Netlyt.Service.Migrations
{
    public partial class OrgSeedNetlyt2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ApiKeys",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "AppId", "AppSecret" },
                values: new object[] { "2a5c672cb6004ca296efee95ce94b46e", "D4uRBIvGFuXmhF/QwMUK6YpcUMiyzo5lFZ7WP46573c=" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ApiKeys",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "AppId", "AppSecret" },
                values: new object[] { "d1b309167fee46cfa7429cddafbcac73", "XAfuwRc6PuaBq8EDYMwmvv/fSHMAUJfTad7dssS30E4=" });
        }
    }
}
