using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Luma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFutureProfileTargetYear : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TargetYear",
                table: "FutureProfiles",
                type: "integer",
                nullable: false,
                defaultValueSql: "EXTRACT(YEAR FROM CURRENT_DATE)::integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TargetYear",
                table: "FutureProfiles");
        }
    }
}
