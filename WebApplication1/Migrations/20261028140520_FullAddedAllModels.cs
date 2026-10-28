using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication1.Migrations
{
    /// <inheritdoc />
    public partial class FullAddedAllModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "ClickLogs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Sub1",
                table: "ClickLogs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Sub2",
                table: "ClickLogs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Sub3",
                table: "ClickLogs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "Token",
                table: "ClickLogs",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "PostbackLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RawToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Payout = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsSuccessful = table.Column<bool>(type: "bit", nullable: false),
                    ResponseMessage = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostbackLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClickLogs_ClickTime",
                table: "ClickLogs",
                column: "ClickTime");

            migrationBuilder.CreateIndex(
                name: "IX_ClickLogs_Token",
                table: "ClickLogs",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PostbackLogs");

            migrationBuilder.DropIndex(
                name: "IX_ClickLogs_ClickTime",
                table: "ClickLogs");

            migrationBuilder.DropIndex(
                name: "IX_ClickLogs_Token",
                table: "ClickLogs");

            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "ClickLogs");

            migrationBuilder.DropColumn(
                name: "Sub1",
                table: "ClickLogs");

            migrationBuilder.DropColumn(
                name: "Sub2",
                table: "ClickLogs");

            migrationBuilder.DropColumn(
                name: "Sub3",
                table: "ClickLogs");

            migrationBuilder.DropColumn(
                name: "Token",
                table: "ClickLogs");
        }
    }
}
