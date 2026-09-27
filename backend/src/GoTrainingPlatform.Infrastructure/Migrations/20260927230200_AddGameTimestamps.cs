using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoTrainingPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGameTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_games_player_id",
                table: "games");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at",
                table: "games",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "games",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.CreateIndex(
                name: "ix_games_player_id_updated_at",
                table: "games",
                columns: new[] { "player_id", "updated_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_games_player_id_updated_at",
                table: "games");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "games");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "games");

            migrationBuilder.CreateIndex(
                name: "ix_games_player_id",
                table: "games",
                column: "player_id");
        }
    }
}
