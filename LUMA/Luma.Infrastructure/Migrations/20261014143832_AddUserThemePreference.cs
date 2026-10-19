using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Luma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserThemePreference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ThemePreference",
                table: "Users",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ThemePreference",
                table: "Users");
        }
    }
}
