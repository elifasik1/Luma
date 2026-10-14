using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Luma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyPlanSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DailyPlanSnapshotId",
                table: "Reflections",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DailyPlanSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DreamId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanDate = table.Column<DateOnly>(type: "date", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyPlanSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyPlanSnapshots_Dreams_DreamId",
                        column: x => x.DreamId,
                        principalTable: "Dreams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DailyPlanActionSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DailyPlanSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    GoalId = table.Column<Guid>(type: "uuid", nullable: true),
                    GoalTitle = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    TimeOfDay = table.Column<string>(type: "text", nullable: false),
                    EstimatedMinutes = table.Column<int>(type: "integer", nullable: false),
                    Intention = table.Column<string>(type: "text", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyPlanActionSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyPlanActionSnapshots_DailyPlanSnapshots_DailyPlanSnapsh~",
                        column: x => x.DailyPlanSnapshotId,
                        principalTable: "DailyPlanSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reflections_DailyPlanSnapshotId",
                table: "Reflections",
                column: "DailyPlanSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyPlanActionSnapshots_DailyPlanSnapshotId_Order",
                table: "DailyPlanActionSnapshots",
                columns: new[] { "DailyPlanSnapshotId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_DailyPlanSnapshots_DreamId_PlanDate",
                table: "DailyPlanSnapshots",
                columns: new[] { "DreamId", "PlanDate" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Reflections_DailyPlanSnapshots_DailyPlanSnapshotId",
                table: "Reflections",
                column: "DailyPlanSnapshotId",
                principalTable: "DailyPlanSnapshots",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reflections_DailyPlanSnapshots_DailyPlanSnapshotId",
                table: "Reflections");

            migrationBuilder.DropTable(
                name: "DailyPlanActionSnapshots");

            migrationBuilder.DropTable(
                name: "DailyPlanSnapshots");

            migrationBuilder.DropIndex(
                name: "IX_Reflections_DailyPlanSnapshotId",
                table: "Reflections");

            migrationBuilder.DropColumn(
                name: "DailyPlanSnapshotId",
                table: "Reflections");
        }
    }
}
