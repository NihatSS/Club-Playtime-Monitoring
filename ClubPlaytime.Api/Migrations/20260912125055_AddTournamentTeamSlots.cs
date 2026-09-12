using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClubPlaytime.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTournamentTeamSlots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirstPlaceName",
                table: "Tournaments",
                type: "TEXT",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondPlaceName",
                table: "Tournaments",
                type: "TEXT",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThirdPlaceName",
                table: "Tournaments",
                type: "TEXT",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Team1Id",
                table: "TournamentMatches",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Team2Id",
                table: "TournamentMatches",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WinnerTeamId",
                table: "TournamentMatches",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TournamentMatches_Team1Id",
                table: "TournamentMatches",
                column: "Team1Id");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentMatches_Team2Id",
                table: "TournamentMatches",
                column: "Team2Id");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentMatches_WinnerTeamId",
                table: "TournamentMatches",
                column: "WinnerTeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_TournamentMatches_TournamentTeams_Team1Id",
                table: "TournamentMatches",
                column: "Team1Id",
                principalTable: "TournamentTeams",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TournamentMatches_TournamentTeams_Team2Id",
                table: "TournamentMatches",
                column: "Team2Id",
                principalTable: "TournamentTeams",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TournamentMatches_TournamentTeams_WinnerTeamId",
                table: "TournamentMatches",
                column: "WinnerTeamId",
                principalTable: "TournamentTeams",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TournamentMatches_TournamentTeams_Team1Id",
                table: "TournamentMatches");

            migrationBuilder.DropForeignKey(
                name: "FK_TournamentMatches_TournamentTeams_Team2Id",
                table: "TournamentMatches");

            migrationBuilder.DropForeignKey(
                name: "FK_TournamentMatches_TournamentTeams_WinnerTeamId",
                table: "TournamentMatches");

            migrationBuilder.DropIndex(
                name: "IX_TournamentMatches_Team1Id",
                table: "TournamentMatches");

            migrationBuilder.DropIndex(
                name: "IX_TournamentMatches_Team2Id",
                table: "TournamentMatches");

            migrationBuilder.DropIndex(
                name: "IX_TournamentMatches_WinnerTeamId",
                table: "TournamentMatches");

            migrationBuilder.DropColumn(
                name: "FirstPlaceName",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "SecondPlaceName",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "ThirdPlaceName",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "Team1Id",
                table: "TournamentMatches");

            migrationBuilder.DropColumn(
                name: "Team2Id",
                table: "TournamentMatches");

            migrationBuilder.DropColumn(
                name: "WinnerTeamId",
                table: "TournamentMatches");
        }
    }
}
