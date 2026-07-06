using IdleGuild.Domain.GameStates;
using IdleGuild.Domain.Requests;
using IdleGuild.Domain.Stages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdleGuild.Infrastructure.Persistence.Configurations;

/// <summary>스테이지 도전 영수증을 PostgreSQL 테이블과 무결성 제약에 매핑합니다.</summary>
public sealed class StageChallengeReceiptConfiguration :
    IEntityTypeConfiguration<StageChallengeReceipt>
{
    public void Configure(
        EntityTypeBuilder<StageChallengeReceipt> builder)
    {
        builder.ToTable(
            "stage_challenge_receipts",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_stage_challenge_receipts_stage_range",
                    "target_stage >= 1 AND target_stage <= 32 AND " +
                    "previous_highest_stage >= 1 AND previous_highest_stage <= 32 AND " +
                    "highest_stage_after >= 1 AND highest_stage_after <= 32");
                table.HasCheckConstraint(
                    "ck_stage_challenge_receipts_outcome",
                    "outcome >= 1 AND outcome <= 4");
                table.HasCheckConstraint(
                    "ck_stage_challenge_receipts_values",
                    "hero_power >= 10 AND required_power >= 10 AND " +
                    "production_bonus_percent_after >= 0 AND " +
                    "checkpoint_gold_awarded >= 0 AND gold_balance_after >= 0");
                table.HasCheckConstraint(
                    "ck_stage_challenge_receipts_consistency",
                    "(outcome = 1 AND target_stage = previous_highest_stage + 1 AND " +
                    "highest_stage_after = target_stage AND hero_power >= required_power) OR " +
                    "(outcome = 2 AND target_stage = previous_highest_stage + 1 AND " +
                    "highest_stage_after = previous_highest_stage AND hero_power < required_power AND checkpoint_gold_awarded = 0) OR " +
                    "(outcome = 3 AND target_stage <= previous_highest_stage AND " +
                    "highest_stage_after = previous_highest_stage AND checkpoint_gold_awarded = 0) OR " +
                    "(outcome = 4 AND target_stage > previous_highest_stage + 1 AND " +
                    "highest_stage_after = previous_highest_stage AND checkpoint_gold_awarded = 0)");
            });

        builder.HasKey(receipt => new
        {
            receipt.PlayerId,
            receipt.IdempotencyKey
        }).HasName("pk_stage_challenge_receipts");

        builder.Property(receipt => receipt.PlayerId)
            .HasColumnName("player_id");
        builder.Property(receipt => receipt.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasMaxLength(IdempotencyPolicy.MaxKeyLength);
        builder.Property(receipt => receipt.TargetStage)
            .HasColumnName("target_stage");
        builder.Property(receipt => receipt.Outcome)
            .HasColumnName("outcome");
        builder.Property(receipt => receipt.PreviousHighestStage)
            .HasColumnName("previous_highest_stage");
        builder.Property(receipt => receipt.HighestStageAfter)
            .HasColumnName("highest_stage_after");
        builder.Property(receipt => receipt.HeroPower)
            .HasColumnName("hero_power");
        builder.Property(receipt => receipt.RequiredPower)
            .HasColumnName("required_power");
        builder.Property(
                receipt =>
                    receipt.ProductionBonusPercentAfter)
            .HasColumnName(
                "production_bonus_percent_after");
        builder.Property(
                receipt =>
                    receipt.CheckpointGoldAwarded)
            .HasColumnName(
                "checkpoint_gold_awarded");
        builder.Property(receipt => receipt.GoldBalanceAfter)
            .HasColumnName("gold_balance_after");
        builder.Property(receipt => receipt.ProcessedAtUtc)
            .HasColumnName("processed_at_utc");

        // 플레이어가 삭제되면 해당 플레이어의 스테이지 영수증도 함께 제거합니다.
        builder.HasOne<PlayerGameState>()
            .WithMany()
            .HasForeignKey(receipt => receipt.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
