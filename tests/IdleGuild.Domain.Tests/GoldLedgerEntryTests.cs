using IdleGuild.Domain.Economy;

namespace IdleGuild.Domain.Tests;

/// <summary>골드 원장이 잔액 변경의 감사 불변식을 지키는지 검증합니다.</summary>
public sealed class GoldLedgerEntryTests
{
    // 올바른 증가 내역은 변경 전·증감량·변경 후 잔액을 그대로 보존해야 합니다.
    [Fact]
    public void Create_WithValidCredit_PreservesAuditValues()
    {
        var playerId = Guid.NewGuid();
        var occurredAt = new DateTimeOffset(
            2026, 7, 10, 1, 0, 0, TimeSpan.FromHours(9));

        var entry = GoldLedgerEntry.Create(
            playerId,
            GoldLedgerReason.IdleRewardClaim,
            balanceBefore: 10,
            amount: 25,
            balanceAfter: 35,
            referenceId: " claim-key ",
            occurredAt);

        Assert.NotEqual(Guid.Empty, entry.EntryId);
        Assert.Equal(playerId, entry.PlayerId);
        Assert.Equal(10, entry.BalanceBefore);
        Assert.Equal(25, entry.Amount);
        Assert.Equal(35, entry.BalanceAfter);
        Assert.Equal("claim-key", entry.ReferenceId);
        Assert.Equal(TimeSpan.Zero, entry.OccurredAtUtc.Offset);
    }

    // 증감량과 두 잔액의 등식이 맞지 않으면 신뢰할 수 없는 원장을 만들 수 없어야 합니다.
    [Fact]
    public void Create_WithMismatchedBalance_Throws()
    {
        var action = () => GoldLedgerEntry.Create(
            Guid.NewGuid(),
            GoldLedgerReason.HeroUpgrade,
            balanceBefore: 100,
            amount: -10,
            balanceAfter: 95,
            referenceId: "upgrade-key",
            DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentException>(action);
    }

    // 실제 잔액 변화가 없는 요청은 기능 영수증으로 추적하고 재화 원장에는 넣지 않아야 합니다.
    [Fact]
    public void Create_WithZeroAmount_Throws()
    {
        var action = () => GoldLedgerEntry.Create(
            Guid.NewGuid(),
            GoldLedgerReason.StageCheckpoint,
            balanceBefore: 100,
            amount: 0,
            balanceAfter: 100,
            referenceId: "stage-key",
            DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentException>(action);
    }
}
