using IdleGuild.Domain.Economy;
using IdleGuild.Domain.GameStates;
using IdleGuild.Domain.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdleGuild.Infrastructure.Persistence.Configurations;

/// <summary>골드 변경 원장의 PostgreSQL 스키마와 감사 무결성 제약을 정의합니다.</summary>
public sealed class GoldLedgerEntryConfiguration :
    IEntityTypeConfiguration<GoldLedgerEntry>
{
    public void Configure(
        EntityTypeBuilder<GoldLedgerEntry> builder)
    {
        builder.ToTable("gold_ledger_entries", table =>
        {
            table.HasCheckConstraint(
                "ck_gold_ledger_entries_reason",
                "reason >= 1 AND reason <= 4");
            table.HasCheckConstraint(
                "ck_gold_ledger_entries_balances",
                "balance_before >= 0 AND balance_after >= 0");
            table.HasCheckConstraint(
                "ck_gold_ledger_entries_amount_non_zero",
                "amount <> 0");
            table.HasCheckConstraint(
                "ck_gold_ledger_entries_balance_equation",
                "balance_after = balance_before + amount");
        });

        builder.HasKey(entry => entry.EntryId)
            .HasName("pk_gold_ledger_entries");

        builder.Property(entry => entry.EntryId)
            .HasColumnName("entry_id")
            .ValueGeneratedNever();
        builder.Property(entry => entry.PlayerId)
            .HasColumnName("player_id");
        builder.Property(entry => entry.Reason)
            .HasColumnName("reason");
        builder.Property(entry => entry.BalanceBefore)
            .HasColumnName("balance_before");
        builder.Property(entry => entry.Amount)
            .HasColumnName("amount");
        builder.Property(entry => entry.BalanceAfter)
            .HasColumnName("balance_after");
        builder.Property(entry => entry.ReferenceId)
            .HasColumnName("reference_id")
            .HasMaxLength(IdempotencyPolicy.MaxKeyLength);
        builder.Property(entry => entry.OccurredAtUtc)
            .HasColumnName("occurred_at_utc");

        // 한 기능의 같은 멱등 요청은 골드 원장에도 한 번만 기록되게 보호합니다.
        builder.HasIndex(entry => new
        {
            entry.PlayerId,
            entry.Reason,
            entry.ReferenceId
        })
            .IsUnique()
            .HasDatabaseName(
                "ux_gold_ledger_entries_player_reason_reference");
        builder.HasIndex(entry => new
        {
            entry.PlayerId,
            entry.OccurredAtUtc,
            entry.EntryId
        })
            .HasDatabaseName(
                "ix_gold_ledger_entries_player_occurred_at_entry");

        // 플레이어 삭제 시 해당 플레이어의 골드 감사 이력도 함께 제거합니다.
        builder.HasOne<PlayerGameState>()
            .WithMany()
            .HasForeignKey(entry => entry.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
