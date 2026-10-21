using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Luma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFutureProfileTargetDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "TargetDate",
                table: "FutureProfiles",
                type: "date",
                nullable: false,
                defaultValueSql: "CURRENT_DATE");

            // Backfill existing rows:
            // 1) If TargetYear is the current year, use CURRENT_DATE so the target date is today (not in the past).
            // 2) If TargetYear is in the future, use January 1st of that year (which is in the future).
            // 3) If TargetYear is in the past, preserve historical intent with January 1st without silently altering TargetYear.
            migrationBuilder.Sql(
                """
                UPDATE "FutureProfiles"
                SET "TargetDate" = CASE
                    WHEN "TargetYear" = (EXTRACT(year FROM CURRENT_DATE))::integer THEN CURRENT_DATE
                    WHEN "TargetYear" > (EXTRACT(year FROM CURRENT_DATE))::integer THEN make_date("TargetYear", 1, 1)
                    WHEN "TargetYear" IS NOT NULL AND "TargetYear" >= 1 THEN make_date("TargetYear", 1, 1)
                    ELSE CURRENT_DATE
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TargetDate",
                table: "FutureProfiles");
        }
    }
}
