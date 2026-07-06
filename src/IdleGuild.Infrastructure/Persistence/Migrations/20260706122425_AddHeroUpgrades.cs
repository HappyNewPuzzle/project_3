using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdleGuild.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHeroUpgrades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "hero_upgrade_receipts",
                columns: table => new
                {
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    outcome = table.Column<int>(type: "integer", nullable: false),
                    previous_level = table.Column<int>(type: "integer", nullable: false),
                    hero_level_after = table.Column<int>(type: "integer", nullable: false),
                    gold_cost = table.Column<long>(type: "bigint", nullable: false),
                    gold_balance_after = table.Column<long>(type: "bigint", nullable: false),
                    processed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hero_upgrade_receipts", x => new { x.player_id, x.idempotency_key });
                    table.CheckConstraint("ck_hero_upgrade_receipts_consistency", "(outcome = 1 AND hero_level_after = previous_level + 1 AND gold_cost > 0) OR (outcome = 2 AND hero_level_after = previous_level AND gold_cost > gold_balance_after) OR (outcome = 3 AND previous_level = 297 AND hero_level_after = 297 AND gold_cost = 0)");
                    table.CheckConstraint("ck_hero_upgrade_receipts_gold", "gold_cost >= 0 AND gold_balance_after >= 0");
                    table.CheckConstraint("ck_hero_upgrade_receipts_levels", "previous_level >= 1 AND hero_level_after >= 1 AND hero_level_after <= 297");
                    table.CheckConstraint("ck_hero_upgrade_receipts_outcome", "outcome >= 1 AND outcome <= 3");
                    table.ForeignKey(
                        name: "FK_hero_upgrade_receipts_player_game_states_player_id",
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
                name: "hero_upgrade_receipts");
        }
    }
}
