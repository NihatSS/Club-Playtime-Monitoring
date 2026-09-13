using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClubPlaytime.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBannerAndPresence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BannerUrl",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSeenOnSite",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSeenOnSite",
                table: "Players",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BannerUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastSeenOnSite",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastSeenOnSite",
                table: "Players");
        }
    }
}
