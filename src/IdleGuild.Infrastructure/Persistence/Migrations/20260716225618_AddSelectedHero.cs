using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdleGuild.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSelectedHero : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "selected_hero_id",
                table: "player_game_states",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "girl");

            migrationBuilder.AddCheckConstraint(
                name: "ck_player_game_states_selected_hero",
                table: "player_game_states",
                sql: "selected_hero_id IN ('girl', 'black_cat', 'classic')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_player_game_states_selected_hero",
                table: "player_game_states");

            migrationBuilder.DropColumn(
                name: "selected_hero_id",
                table: "player_game_states");
        }
    }
}
