using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdleGuild.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLongTermProgression : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "attack_level",
                table: "player_game_states",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "attack_speed_level",
                table: "player_game_states",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "critical_level",
                table: "player_game_states",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "prestige_level",
                table: "player_game_states",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "soul_stones",
                table: "player_game_states",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "attack_level",
                table: "player_game_states");

            migrationBuilder.DropColumn(
                name: "attack_speed_level",
                table: "player_game_states");

            migrationBuilder.DropColumn(
                name: "critical_level",
                table: "player_game_states");

            migrationBuilder.DropColumn(
                name: "prestige_level",
                table: "player_game_states");

            migrationBuilder.DropColumn(
                name: "soul_stones",
                table: "player_game_states");
        }
    }
}
