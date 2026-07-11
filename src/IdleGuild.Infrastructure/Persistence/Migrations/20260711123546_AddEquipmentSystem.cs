using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdleGuild.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "equipment_change_receipts",
                columns: table => new
                {
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    equipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    desired_equipped = table.Column<bool>(type: "boolean", nullable: false),
                    outcome = table.Column<int>(type: "integer", nullable: false),
                    replaced_equipment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    processed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_equipment_change_receipts", x => new { x.player_id, x.idempotency_key });
                    table.CheckConstraint("ck_equipment_change_receipts_outcome", "outcome >= 1 AND outcome <= 2");
                    table.CheckConstraint("ck_equipment_change_receipts_replacement", "desired_equipped OR replaced_equipment_id IS NULL");
                    table.ForeignKey(
                        name: "FK_equipment_change_receipts_player_game_states_player_id",
                        column: x => x.player_id,
                        principalTable: "player_game_states",
                        principalColumn: "player_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "player_equipment",
                columns: table => new
                {
                    equipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    definition_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    slot = table.Column<int>(type: "integer", nullable: false),
                    is_equipped = table.Column<bool>(type: "boolean", nullable: false),
                    acquired_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_player_equipment", x => x.equipment_id);
                    table.CheckConstraint("ck_player_equipment_slot", "slot >= 1 AND slot <= 2");
                    table.ForeignKey(
                        name: "FK_player_equipment_player_game_states_player_id",
                        column: x => x.player_id,
                        principalTable: "player_game_states",
                        principalColumn: "player_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_player_equipment_player",
                table: "player_equipment",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "ux_player_equipment_equipped_slot",
                table: "player_equipment",
                columns: new[] { "player_id", "slot" },
                unique: true,
                filter: "is_equipped");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "equipment_change_receipts");

            migrationBuilder.DropTable(
                name: "player_equipment");
        }
    }
}
