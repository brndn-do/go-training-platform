using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoTrainingPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "games",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_color = table.Column<int>(type: "integer", nullable: false),
                    board_size = table.Column<int>(type: "integer", nullable: false),
                    komi = table.Column<double>(type: "double precision", nullable: false),
                    outcome = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_games", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "moves",
                columns: table => new
                {
                    move_number = table.Column<int>(type: "integer", nullable: false),
                    game_id = table.Column<Guid>(type: "uuid", nullable: false),
                    coordinates_x = table.Column<int>(type: "integer", nullable: true),
                    coordinates_y = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_moves", x => new { x.game_id, x.move_number });
                    table.ForeignKey(
                        name: "fk_moves_games_game_id",
                        column: x => x.game_id,
                        principalTable: "games",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "moves");

            migrationBuilder.DropTable(
                name: "games");
        }
    }
}
