using Luma.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Luma.Infrastructure.Migrations;

[DbContext(typeof(LumaDbContext))]
[Migration("20261005170500_AddUserAuthentication")]
public partial class AddUserAuthentication : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "PasswordHash",
            table: "Users",
            type: "text",
            nullable: true);

        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM "Users"
                    WHERE "Email" IS NOT NULL
                    GROUP BY lower(trim("Email"))
                    HAVING count(*) > 1
                ) THEN
                    RAISE EXCEPTION 'Cannot add case-insensitive email uniqueness: duplicate legacy email values exist.';
                END IF;
            END $$;

            CREATE UNIQUE INDEX "IX_Users_Email_Lower"
            ON "Users" (lower(trim("Email")))
            WHERE "Email" IS NOT NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_Users_Email_Lower";""");
        migrationBuilder.DropColumn(
            name: "PasswordHash",
            table: "Users");
    }
}
