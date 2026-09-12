using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClubPlaytime.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTournamentRulesText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RulesText",
                table: "Tournaments",
                type: "TEXT",
                maxLength: 4000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RulesText",
                table: "Tournaments");
        }
    }
}
