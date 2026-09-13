using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClubPlaytime.Api.Migrations;

public sealed partial class AddBannerImageAndGameEvents : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Uploaded banner image bytes (null = not uploaded; URL option stays in BannerUrl).
        migrationBuilder.AddColumn<byte[]>(
            name: "BannerImage",
            table: "Users",
            type: "BLOB",
            nullable: true);

        // Per-game activity: which game an event refers to (null = legacy rows,
        // which are always the target game) and the Roblox place ID for icon lookup.
        migrationBuilder.AddColumn<string>(
            name: "GameName",
            table: "PlayerActivityEvents",
            type: "TEXT",
            maxLength: 150,
            nullable: true);

        migrationBuilder.AddColumn<long>(
            name: "PlaceId",
            table: "PlayerActivityEvents",
            type: "INTEGER",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "BannerImage",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "PlaceId",
            table: "PlayerActivityEvents");

        migrationBuilder.DropColumn(
            name: "GameName",
            table: "PlayerActivityEvents");
    }
}
