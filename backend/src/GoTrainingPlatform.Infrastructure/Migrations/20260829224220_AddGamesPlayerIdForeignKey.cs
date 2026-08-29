using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoTrainingPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGamesPlayerIdForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_games_player_id",
                table: "games",
                column: "player_id");

            migrationBuilder.AddForeignKey(
                name: "fk_games_users_player_id",
                table: "games",
                column: "player_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_games_users_player_id",
                table: "games");

            migrationBuilder.DropIndex(
                name: "ix_games_player_id",
                table: "games");
        }
    }
}
