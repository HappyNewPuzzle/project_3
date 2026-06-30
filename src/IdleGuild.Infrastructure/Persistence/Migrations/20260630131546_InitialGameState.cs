using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdleGuild.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialGameState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "player_game_states",
                columns: table => new
                {
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    gold = table.Column<long>(type: "bigint", nullable: false),
                    hero_level = table.Column<int>(type: "integer", nullable: false),
                    highest_stage = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_idle_reward_claimed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_player_game_states", x => x.player_id);
                    table.CheckConstraint("ck_player_game_states_gold_non_negative", "gold >= 0");
                    table.CheckConstraint("ck_player_game_states_hero_level_positive", "hero_level >= 1");
                    table.CheckConstraint("ck_player_game_states_highest_stage_positive", "highest_stage >= 1");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "player_game_states");
        }
    }
}
