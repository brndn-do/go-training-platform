using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoTrainingPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBotStrengthToGame : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "bot_strength",
                table: "games",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bot_strength",
                table: "games");
        }
    }
}
