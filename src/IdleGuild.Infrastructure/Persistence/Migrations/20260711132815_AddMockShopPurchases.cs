using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdleGuild.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMockShopPurchases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_gold_ledger_entries_reason",
                table: "gold_ledger_entries");

            migrationBuilder.CreateTable(
                name: "shop_purchase_receipts",
                columns: table => new
                {
                    purchase_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    product_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    mock_price = table.Column<int>(type: "integer", nullable: false),
                    gold_awarded = table.Column<long>(type: "bigint", nullable: false),
                    gold_balance_after = table.Column<long>(type: "bigint", nullable: false),
                    purchased_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shop_purchase_receipts", x => x.purchase_id);
                    table.CheckConstraint("ck_shop_purchase_receipts_gold", "gold_awarded > 0 AND gold_balance_after >= gold_awarded");
                    table.CheckConstraint("ck_shop_purchase_receipts_price", "mock_price > 0");
                    table.ForeignKey(
                        name: "FK_shop_purchase_receipts_player_game_states_player_id",
                        column: x => x.player_id,
                        principalTable: "player_game_states",
                        principalColumn: "player_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_gold_ledger_entries_reason",
                table: "gold_ledger_entries",
                sql: "reason >= 1 AND reason <= 4");

            migrationBuilder.CreateIndex(
                name: "ix_shop_purchase_receipts_player_time",
                table: "shop_purchase_receipts",
                columns: new[] { "player_id", "purchased_at_utc", "purchase_id" });

            migrationBuilder.CreateIndex(
                name: "ux_shop_purchase_receipts_player_key",
                table: "shop_purchase_receipts",
                columns: new[] { "player_id", "idempotency_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "shop_purchase_receipts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_gold_ledger_entries_reason",
                table: "gold_ledger_entries");

            migrationBuilder.AddCheckConstraint(
                name: "ck_gold_ledger_entries_reason",
                table: "gold_ledger_entries",
                sql: "reason >= 1 AND reason <= 3");
        }
    }
}
