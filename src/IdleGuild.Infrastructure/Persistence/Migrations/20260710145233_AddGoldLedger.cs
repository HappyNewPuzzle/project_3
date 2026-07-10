using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdleGuild.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGoldLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gold_ledger_entries",
                columns: table => new
                {
                    entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<int>(type: "integer", nullable: false),
                    balance_before = table.Column<long>(type: "bigint", nullable: false),
                    amount = table.Column<long>(type: "bigint", nullable: false),
                    balance_after = table.Column<long>(type: "bigint", nullable: false),
                    reference_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gold_ledger_entries", x => x.entry_id);
                    table.CheckConstraint("ck_gold_ledger_entries_amount_non_zero", "amount <> 0");
                    table.CheckConstraint("ck_gold_ledger_entries_balance_equation", "balance_after = balance_before + amount");
                    table.CheckConstraint("ck_gold_ledger_entries_balances", "balance_before >= 0 AND balance_after >= 0");
                    table.CheckConstraint("ck_gold_ledger_entries_reason", "reason >= 1 AND reason <= 3");
                    table.ForeignKey(
                        name: "FK_gold_ledger_entries_player_game_states_player_id",
                        column: x => x.player_id,
                        principalTable: "player_game_states",
                        principalColumn: "player_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_gold_ledger_entries_player_occurred_at",
                table: "gold_ledger_entries",
                columns: new[] { "player_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_gold_ledger_entries_player_reason_reference",
                table: "gold_ledger_entries",
                columns: new[] { "player_id", "reason", "reference_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gold_ledger_entries");
        }
    }
}
