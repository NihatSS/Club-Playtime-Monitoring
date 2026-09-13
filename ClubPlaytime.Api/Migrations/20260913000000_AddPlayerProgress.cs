using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClubPlaytime.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayerAchievements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    AchievementKey = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    UnlockedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Detail = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerAchievements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerAchievements_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerStreaks",
                columns: table => new
                {
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentStreak = table.Column<int>(type: "INTEGER", nullable: false),
                    LongestStreak = table.Column<int>(type: "INTEGER", nullable: false),
                    DaysPlayed = table.Column<int>(type: "INTEGER", nullable: false),
                    LastActiveDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerStreaks", x => x.PlayerId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerAchievements_PlayerId",
                table: "PlayerAchievements",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerAchievements_PlayerId_AchievementKey",
                table: "PlayerAchievements",
                columns: new[] { "PlayerId", "AchievementKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerAchievements");

            migrationBuilder.DropTable(
                name: "PlayerStreaks");
        }
    }
}
