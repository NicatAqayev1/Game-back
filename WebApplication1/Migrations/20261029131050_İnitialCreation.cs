using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication1.Migrations
{
    /// <inheritdoc />
    public partial class İnitialCreation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Casinos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LogoText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LogoColor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rating = table.Column<decimal>(type: "decimal(3,1)", precision: 3, scale: 1, nullable: false),
                    BonusTitle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BonusValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PromoCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AffiliateUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Categories = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    AllowedIPs = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Casinos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClickLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Token = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CasinoId = table.Column<int>(type: "int", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CountryCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClickTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRegistered = table.Column<bool>(type: "bit", nullable: false),
                    IsDeposited = table.Column<bool>(type: "bit", nullable: false),
                    Payout = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Sub1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Sub2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Sub3 = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClickLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClickLogs_Casinos_CasinoId",
                        column: x => x.CasinoId,
                        principalTable: "Casinos",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PostbackLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClickLogId = table.Column<long>(type: "bigint", nullable: true),
                    CasinoId = table.Column<int>(type: "int", nullable: true),
                    PostbackType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Payout = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RawData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TransactionId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsSuccessful = table.Column<bool>(type: "bit", nullable: false),
                    ResponseMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProcessedTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostbackLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostbackLogs_ClickLogs_ClickLogId",
                        column: x => x.ClickLogId,
                        principalTable: "ClickLogs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Casinos_Slug",
                table: "Casinos",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClickLogs_CasinoId",
                table: "ClickLogs",
                column: "CasinoId");

            migrationBuilder.CreateIndex(
                name: "IX_ClickLogs_ClickTime",
                table: "ClickLogs",
                column: "ClickTime");

            migrationBuilder.CreateIndex(
                name: "IX_ClickLogs_Token",
                table: "ClickLogs",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostbackLogs_CasinoId_TransactionId",
                table: "PostbackLogs",
                columns: new[] { "CasinoId", "TransactionId" },
                unique: true,
                filter: "[TransactionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PostbackLogs_ClickLogId",
                table: "PostbackLogs",
                column: "ClickLogId");

            migrationBuilder.CreateIndex(
                name: "IX_PostbackLogs_TransactionId_CasinoId",
                table: "PostbackLogs",
                columns: new[] { "TransactionId", "CasinoId" },
                unique: true,
                filter: "[IsSuccessful] = 1 AND [TransactionId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PostbackLogs");

            migrationBuilder.DropTable(
                name: "ClickLogs");

            migrationBuilder.DropTable(
                name: "Casinos");
        }
    }
}
