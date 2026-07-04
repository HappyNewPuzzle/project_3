using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdleGuild.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIdleRewardClaims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "idle_reward_claim_receipts",
                columns: table => new
                {
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    gold_awarded = table.Column<long>(type: "bigint", nullable: false),
                    accumulated_seconds = table.Column<int>(type: "integer", nullable: false),
                    gold_balance_after = table.Column<long>(type: "bigint", nullable: false),
                    claimed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idle_reward_claim_receipts", x => new { x.player_id, x.idempotency_key });
                    table.CheckConstraint("ck_idle_reward_claim_receipts_accumulated_seconds", "accumulated_seconds >= 0 AND accumulated_seconds <= 28800");
                    table.CheckConstraint("ck_idle_reward_claim_receipts_gold_awarded", "gold_awarded >= 0");
                    table.CheckConstraint("ck_idle_reward_claim_receipts_gold_balance_after", "gold_balance_after >= 0");
                    table.ForeignKey(
                        name: "FK_idle_reward_claim_receipts_player_game_states_player_id",
                        column: x => x.player_id,
                        principalTable: "player_game_states",
                        principalColumn: "player_id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "idle_reward_claim_receipts");
        }
    }
}
