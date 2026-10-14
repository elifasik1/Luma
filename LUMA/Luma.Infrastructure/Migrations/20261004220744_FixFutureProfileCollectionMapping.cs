using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Luma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixFutureProfileCollectionMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""FutureProfiles""
                ALTER COLUMN ""Lifestyle"" TYPE jsonb
                USING CASE
                    WHEN ""Lifestyle"" = '' THEN '[]'::jsonb
                    ELSE to_json(array_remove(string_to_array(""Lifestyle"", ';'), ''))::jsonb
                END;

                ALTER TABLE ""FutureProfiles""
                ALTER COLUMN ""Aspirations"" TYPE jsonb
                USING CASE
                    WHEN ""Aspirations"" = '' THEN '[]'::jsonb
                    ELSE to_json(array_remove(string_to_array(""Aspirations"", ';'), ''))::jsonb
                END;

                ALTER TABLE ""FutureProfiles""
                ALTER COLUMN ""Values"" TYPE jsonb
                USING CASE
                    WHEN ""Values"" = '' THEN '[]'::jsonb
                    ELSE to_json(array_remove(string_to_array(""Values"", ';'), ''))::jsonb
                END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""FutureProfiles""
                ALTER COLUMN ""Lifestyle"" TYPE text
                USING array_to_string(COALESCE(CAST(""Lifestyle"" AS text[]), ARRAY[]::text[]), ';');

                ALTER TABLE ""FutureProfiles""
                ALTER COLUMN ""Aspirations"" TYPE text
                USING array_to_string(COALESCE(CAST(""Aspirations"" AS text[]), ARRAY[]::text[]), ';');

                ALTER TABLE ""FutureProfiles""
                ALTER COLUMN ""Values"" TYPE text
                USING array_to_string(COALESCE(CAST(""Values"" AS text[]), ARRAY[]::text[]), ';');
            ");
        }
    }
}
