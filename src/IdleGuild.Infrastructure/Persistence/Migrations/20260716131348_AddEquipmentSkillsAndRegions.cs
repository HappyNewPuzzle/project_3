using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdleGuild.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentSkillsAndRegions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "equipment_count",
                table: "player_game_states",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "equipment_tier",
                table: "player_game_states",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "skill_one_level",
                table: "player_game_states",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "skill_three_level",
                table: "player_game_states",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "skill_two_level",
                table: "player_game_states",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "unlocked_region",
                table: "player_game_states",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "equipment_count",
                table: "player_game_states");

            migrationBuilder.DropColumn(
                name: "equipment_tier",
                table: "player_game_states");

            migrationBuilder.DropColumn(
                name: "skill_one_level",
                table: "player_game_states");

            migrationBuilder.DropColumn(
                name: "skill_three_level",
                table: "player_game_states");

            migrationBuilder.DropColumn(
                name: "skill_two_level",
                table: "player_game_states");

            migrationBuilder.DropColumn(
                name: "unlocked_region",
                table: "player_game_states");
        }
    }
}
