using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Luma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReflectionPlanDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "PlanDate",
                table: "Reflections",
                type: "date",
                nullable: true);

            migrationBuilder.Sql("UPDATE \"Reflections\" SET \"PlanDate\" = (\"CreatedAt\" AT TIME ZONE 'UTC')::date WHERE \"PlanDate\" IS NULL;");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "PlanDate",
                table: "Reflections",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reflections_DreamId_PlanDate",
                table: "Reflections",
                columns: new[] { "DreamId", "PlanDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reflections_DreamId_PlanDate",
                table: "Reflections");

            migrationBuilder.DropColumn(
                name: "PlanDate",
                table: "Reflections");
        }
    }
}
