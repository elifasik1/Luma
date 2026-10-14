using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Luma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFutureProfileIdentityAndControlledImagery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "ThemeConfigs"
                ALTER COLUMN "Imagery" TYPE jsonb
                USING CASE
                    WHEN "Imagery" IS NULL OR btrim("Imagery") = '' THEN '[]'::jsonb
                    WHEN lower("Imagery") IN ('city', 'big_city') THEN '["city"]'::jsonb
                    WHEN lower("Imagery") IN ('nature', 'forest') THEN '["nature"]'::jsonb
                    WHEN lower("Imagery") = 'ocean' THEN '["ocean"]'::jsonb
                    WHEN lower("Imagery") = 'campus' THEN '["campus"]'::jsonb
                    WHEN lower("Imagery") = 'workspace' THEN '["workspace"]'::jsonb
                    WHEN lower("Imagery") = 'bakery' THEN '["bakery"]'::jsonb
                    WHEN lower("Imagery") = 'travel' THEN '["travel"]'::jsonb
                    WHEN lower("Imagery") = 'home' THEN '["home"]'::jsonb
                    WHEN lower("Imagery") = 'books' THEN '["books"]'::jsonb
                    WHEN lower("Imagery") = 'technology' THEN '["technology"]'::jsonb
                    ELSE '["home"]'::jsonb
                END;
                """);

            migrationBuilder.AddColumn<string>(
                name: "Identity",
                table: "FutureProfiles",
                type: "text",
                nullable: false,
                defaultValue: "Dreamer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Identity",
                table: "FutureProfiles");

            migrationBuilder.Sql("""
                ALTER TABLE "ThemeConfigs"
                ALTER COLUMN "Imagery" TYPE text
                USING CASE COALESCE("Imagery" ->> 0, 'home')
                    WHEN 'city' THEN 'big_city'
                    WHEN 'nature' THEN 'forest'
                    ELSE COALESCE("Imagery" ->> 0, 'home')
                END;
                """);
        }
    }
}
