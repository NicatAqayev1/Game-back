using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication1.Migrations
{
    /// <inheritdoc />
    public partial class ClickLogCasinoRelated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "ClickLogs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ClickLogs_CasinoId",
                table: "ClickLogs",
                column: "CasinoId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClickLogs_Casinos_CasinoId",
                table: "ClickLogs",
                column: "CasinoId",
                principalTable: "Casinos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClickLogs_Casinos_CasinoId",
                table: "ClickLogs");

            migrationBuilder.DropIndex(
                name: "IX_ClickLogs_CasinoId",
                table: "ClickLogs");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "ClickLogs");
        }
    }
}
