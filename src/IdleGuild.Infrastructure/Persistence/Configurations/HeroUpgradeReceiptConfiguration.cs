using IdleGuild.Domain.GameStates;
using IdleGuild.Domain.Heroes;
using IdleGuild.Domain.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdleGuild.Infrastructure.Persistence.Configurations;

/// <summary>영웅 강화 영수증을 PostgreSQL 테이블과 무결성 제약에 매핑합니다.</summary>
public sealed class HeroUpgradeReceiptConfiguration :
    IEntityTypeConfiguration<HeroUpgradeReceipt>
{
    public void Configure(
        EntityTypeBuilder<HeroUpgradeReceipt> builder)
    {
        builder.ToTable(
            "hero_upgrade_receipts",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_hero_upgrade_receipts_outcome",
                    "outcome >= 1 AND outcome <= 3");
                table.HasCheckConstraint(
                    "ck_hero_upgrade_receipts_levels",
                    "previous_level >= 1 AND hero_level_after >= 1 AND hero_level_after <= 297");
                table.HasCheckConstraint(
                    "ck_hero_upgrade_receipts_gold",
                    "gold_cost >= 0 AND gold_balance_after >= 0");
                table.HasCheckConstraint(
                    "ck_hero_upgrade_receipts_consistency",
                    "(outcome = 1 AND hero_level_after = previous_level + 1 AND gold_cost > 0) OR " +
                    "(outcome = 2 AND hero_level_after = previous_level AND gold_cost > gold_balance_after) OR " +
                    "(outcome = 3 AND previous_level = 297 AND hero_level_after = 297 AND gold_cost = 0)");
            });

        builder.HasKey(receipt => new
        {
            receipt.PlayerId,
            receipt.IdempotencyKey
        }).HasName("pk_hero_upgrade_receipts");

        builder.Property(receipt => receipt.PlayerId)
            .HasColumnName("player_id");
        builder.Property(receipt => receipt.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasMaxLength(IdempotencyPolicy.MaxKeyLength);
        builder.Property(receipt => receipt.Outcome)
            .HasColumnName("outcome");
        builder.Property(receipt => receipt.PreviousLevel)
            .HasColumnName("previous_level");
        builder.Property(receipt => receipt.HeroLevelAfter)
            .HasColumnName("hero_level_after");
        builder.Property(receipt => receipt.GoldCost)
            .HasColumnName("gold_cost");
        builder.Property(receipt => receipt.GoldBalanceAfter)
            .HasColumnName("gold_balance_after");
        builder.Property(receipt => receipt.ProcessedAtUtc)
            .HasColumnName("processed_at_utc");

        // 플레이어가 삭제되면 해당 플레이어의 강화 영수증도 함께 제거합니다.
        builder.HasOne<PlayerGameState>()
            .WithMany()
            .HasForeignKey(receipt => receipt.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
