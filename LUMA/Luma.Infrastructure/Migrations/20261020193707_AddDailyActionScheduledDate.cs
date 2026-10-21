using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Luma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyActionScheduledDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "ScheduledDate",
                table: "DailyActions",
                type: "date",
                nullable: true);
            migrationBuilder.Sql("UPDATE \"DailyActions\" SET \"ScheduledDate\" = (\"GeneratedAt\" AT TIME ZONE 'UTC')::date WHERE \"ScheduledDate\" IS NULL;");
            migrationBuilder.AlterColumn<DateOnly>(
                name: "ScheduledDate",
                table: "DailyActions",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ScheduledDate", table: "DailyActions");
        }
    }
}
