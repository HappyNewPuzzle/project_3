using IdleGuild.Domain.GameStates;
using IdleGuild.Domain.Requests;
using IdleGuild.Domain.Rewards;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdleGuild.Infrastructure.Persistence.Configurations;

/// <summary>방치 보상 영수증을 PostgreSQL 테이블에 매핑합니다.</summary>
public sealed class IdleRewardClaimReceiptConfiguration :
    IEntityTypeConfiguration<IdleRewardClaimReceipt>
{
    public void Configure(
        EntityTypeBuilder<IdleRewardClaimReceipt> builder)
    {
        builder.ToTable(
            "idle_reward_claim_receipts",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_idle_reward_claim_receipts_gold_awarded",
                    "gold_awarded >= 0");
                table.HasCheckConstraint(
                    "ck_idle_reward_claim_receipts_accumulated_seconds",
                    "accumulated_seconds >= 0 AND accumulated_seconds <= 28800");
                table.HasCheckConstraint(
                    "ck_idle_reward_claim_receipts_gold_balance_after",
                    "gold_balance_after >= 0");
            });

        builder.HasKey(receipt => new
        {
            receipt.PlayerId,
            receipt.IdempotencyKey
        });

        builder.Property(receipt => receipt.PlayerId)
            .HasColumnName("player_id");
        builder.Property(receipt => receipt.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasMaxLength(IdempotencyPolicy.MaxKeyLength);
        builder.Property(receipt => receipt.GoldAwarded)
            .HasColumnName("gold_awarded");
        builder.Property(receipt => receipt.AccumulatedSeconds)
            .HasColumnName("accumulated_seconds");
        builder.Property(receipt => receipt.GoldBalanceAfter)
            .HasColumnName("gold_balance_after");
        builder.Property(receipt => receipt.ClaimedAtUtc)
            .HasColumnName("claimed_at_utc");

        // 플레이어 삭제 시 해당 플레이어의 정산 영수증도 함께 제거합니다.
        builder.HasOne<PlayerGameState>()
            .WithMany()
            .HasForeignKey(receipt => receipt.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
