using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdleGuild.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStageProgression : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_player_game_states_highest_stage_positive",
                table: "player_game_states");

            migrationBuilder.AddColumn<int>(
                name: "idle_reward_remainder_hundredths",
                table: "player_game_states",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "production_percent",
                table: "idle_reward_claim_receipts",
                type: "integer",
                nullable: false,
                defaultValue: 100);

            migrationBuilder.AddColumn<int>(
                name: "remainder_hundredths",
                table: "idle_reward_claim_receipts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "stage_challenge_receipts",
                columns: table => new
                {
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    target_stage = table.Column<int>(type: "integer", nullable: false),
                    outcome = table.Column<int>(type: "integer", nullable: false),
                    previous_highest_stage = table.Column<int>(type: "integer", nullable: false),
                    highest_stage_after = table.Column<int>(type: "integer", nullable: false),
                    hero_power = table.Column<int>(type: "integer", nullable: false),
                    required_power = table.Column<int>(type: "integer", nullable: false),
                    production_bonus_percent_after = table.Column<int>(type: "integer", nullable: false),
                    checkpoint_gold_awarded = table.Column<long>(type: "bigint", nullable: false),
                    gold_balance_after = table.Column<long>(type: "bigint", nullable: false),
                    processed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stage_challenge_receipts", x => new { x.player_id, x.idempotency_key });
                    table.CheckConstraint("ck_stage_challenge_receipts_consistency", "(outcome = 1 AND target_stage = previous_highest_stage + 1 AND highest_stage_after = target_stage AND hero_power >= required_power) OR (outcome = 2 AND target_stage = previous_highest_stage + 1 AND highest_stage_after = previous_highest_stage AND hero_power < required_power AND checkpoint_gold_awarded = 0) OR (outcome = 3 AND target_stage <= previous_highest_stage AND highest_stage_after = previous_highest_stage AND checkpoint_gold_awarded = 0) OR (outcome = 4 AND target_stage > previous_highest_stage + 1 AND highest_stage_after = previous_highest_stage AND checkpoint_gold_awarded = 0)");
                    table.CheckConstraint("ck_stage_challenge_receipts_outcome", "outcome >= 1 AND outcome <= 4");
                    table.CheckConstraint("ck_stage_challenge_receipts_stage_range", "target_stage >= 1 AND target_stage <= 32 AND previous_highest_stage >= 1 AND previous_highest_stage <= 32 AND highest_stage_after >= 1 AND highest_stage_after <= 32");
                    table.CheckConstraint("ck_stage_challenge_receipts_values", "hero_power >= 10 AND required_power >= 10 AND production_bonus_percent_after >= 0 AND checkpoint_gold_awarded >= 0 AND gold_balance_after >= 0");
                    table.ForeignKey(
                        name: "FK_stage_challenge_receipts_player_game_states_player_id",
                        column: x => x.player_id,
                        principalTable: "player_game_states",
                        principalColumn: "player_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_player_game_states_highest_stage_positive",
                table: "player_game_states",
                sql: "highest_stage >= 1 AND highest_stage <= 32");

            migrationBuilder.AddCheckConstraint(
                name: "ck_player_game_states_idle_reward_remainder",
                table: "player_game_states",
                sql: "idle_reward_remainder_hundredths >= 0 AND idle_reward_remainder_hundredths < 100");

            migrationBuilder.AddCheckConstraint(
                name: "ck_idle_reward_claim_receipts_production_percent",
                table: "idle_reward_claim_receipts",
                sql: "production_percent >= 100");

            migrationBuilder.AddCheckConstraint(
                name: "ck_idle_reward_claim_receipts_remainder",
                table: "idle_reward_claim_receipts",
                sql: "remainder_hundredths >= 0 AND remainder_hundredths < 100");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stage_challenge_receipts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_player_game_states_highest_stage_positive",
                table: "player_game_states");

            migrationBuilder.DropCheckConstraint(
                name: "ck_player_game_states_idle_reward_remainder",
                table: "player_game_states");

            migrationBuilder.DropCheckConstraint(
                name: "ck_idle_reward_claim_receipts_production_percent",
                table: "idle_reward_claim_receipts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_idle_reward_claim_receipts_remainder",
                table: "idle_reward_claim_receipts");

            migrationBuilder.DropColumn(
                name: "idle_reward_remainder_hundredths",
                table: "player_game_states");

            migrationBuilder.DropColumn(
                name: "production_percent",
                table: "idle_reward_claim_receipts");

            migrationBuilder.DropColumn(
                name: "remainder_hundredths",
                table: "idle_reward_claim_receipts");

            migrationBuilder.AddCheckConstraint(
                name: "ck_player_game_states_highest_stage_positive",
                table: "player_game_states",
                sql: "highest_stage >= 1");
        }
    }
}
