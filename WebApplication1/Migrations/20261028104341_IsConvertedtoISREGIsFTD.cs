using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication1.Migrations
{
    /// <inheritdoc />
    public partial class IsConvertedtoISREGIsFTD : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsConverted",
                table: "ClickLogs",
                newName: "IsRegistered");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeposited",
                table: "ClickLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeposited",
                table: "ClickLogs");

            migrationBuilder.RenameColumn(
                name: "IsRegistered",
                table: "ClickLogs",
                newName: "IsConverted");
        }
    }
}
