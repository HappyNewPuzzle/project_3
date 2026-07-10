using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdleGuild.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeGoldLedgerAdminQuery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_gold_ledger_entries_player_occurred_at",
                table: "gold_ledger_entries");

            migrationBuilder.CreateIndex(
                name: "ix_gold_ledger_entries_player_occurred_at_entry",
                table: "gold_ledger_entries",
                columns: new[] { "player_id", "occurred_at_utc", "entry_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_gold_ledger_entries_player_occurred_at_entry",
                table: "gold_ledger_entries");

            migrationBuilder.CreateIndex(
                name: "ix_gold_ledger_entries_player_occurred_at",
                table: "gold_ledger_entries",
                columns: new[] { "player_id", "occurred_at_utc" });
        }
    }
}
