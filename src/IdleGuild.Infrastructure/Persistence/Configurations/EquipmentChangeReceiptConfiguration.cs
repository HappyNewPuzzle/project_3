using IdleGuild.Domain.Equipment;
using IdleGuild.Domain.GameStates;
using IdleGuild.Domain.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdleGuild.Infrastructure.Persistence.Configurations;

/// <summary>장비 장착 상태 변경 영수증과 결과 제약을 PostgreSQL에 매핑합니다.</summary>
public sealed class EquipmentChangeReceiptConfiguration :
    IEntityTypeConfiguration<EquipmentChangeReceipt>
{
    public void Configure(
        EntityTypeBuilder<EquipmentChangeReceipt> builder)
    {
        builder.ToTable("equipment_change_receipts", table =>
        {
            table.HasCheckConstraint(
                "ck_equipment_change_receipts_outcome",
                "outcome >= 1 AND outcome <= 2");
            table.HasCheckConstraint(
                "ck_equipment_change_receipts_replacement",
                "desired_equipped OR replaced_equipment_id IS NULL");
        });

        builder.HasKey(receipt => new
        {
            receipt.PlayerId,
            receipt.IdempotencyKey
        }).HasName("pk_equipment_change_receipts");
        builder.Property(receipt => receipt.PlayerId)
            .HasColumnName("player_id");
        builder.Property(receipt => receipt.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasMaxLength(IdempotencyPolicy.MaxKeyLength);
        builder.Property(receipt => receipt.EquipmentId)
            .HasColumnName("equipment_id");
        builder.Property(receipt => receipt.DesiredEquipped)
            .HasColumnName("desired_equipped");
        builder.Property(receipt => receipt.Outcome)
            .HasColumnName("outcome");
        builder.Property(receipt => receipt.ReplacedEquipmentId)
            .HasColumnName("replaced_equipment_id");
        builder.Property(receipt => receipt.ProcessedAtUtc)
            .HasColumnName("processed_at_utc");

        builder.HasOne<PlayerGameState>()
            .WithMany()
            .HasForeignKey(receipt => receipt.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
