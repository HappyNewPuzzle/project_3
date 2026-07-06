using IdleGuild.Domain.GameStates;
using IdleGuild.Domain.Stages;

namespace IdleGuild.Domain.Tests;

/// <summary>결정론적 스테이지 판정, 진행 순서, 생산 보너스 정산을 검증합니다.</summary>
public sealed class StageChallengeTests
{
    // 20% 복리 요구 전투력을 부동소수점 없이 내림 계산해야 합니다.
    [Theory]
    [InlineData(1, 10)]
    [InlineData(2, 12)]
    [InlineData(3, 14)]
    [InlineData(4, 17)]
    [InlineData(32, 2_848)]
    public void CalculateRequiredPower_ReturnsExpectedFloor(
        int stage,
        int expectedPower)
    {
        var power =
            StageChallengePolicy.CalculateRequiredPower(
                stage);

        Assert.Equal(expectedPower, power);
    }

    // 레벨 1 영웅은 전투력 10으로 요구 전투력 12인 스테이지 2에 실패해야 합니다.
    [Fact]
    public void ChallengeStage_WithLowPower_KeepsProgress()
    {
        var state = PlayerGameState.Create(
            Guid.NewGuid(),
            Utc(0));

        var result = state.ChallengeStage(
            targetStage: 2,
            Utc(1));

        Assert.Equal(
            StageChallengeOutcome.InsufficientPower,
            result.Outcome);
        Assert.Equal(10, result.HeroPower);
        Assert.Equal(12, result.RequiredPower);
        Assert.Equal(1, state.HighestStage);
        Assert.Equal(0, result.CheckpointGoldAwarded);
    }

    // 바로 다음 스테이지를 건너뛴 도전은 전투력과 관계없이 잠금 상태여야 합니다.
    [Fact]
    public void ChallengeStage_SkippingNextStage_IsLocked()
    {
        var state = PlayerGameState.Create(
            Guid.NewGuid(),
            Utc(0));

        var result = state.ChallengeStage(
            targetStage: 3,
            Utc(1));

        Assert.Equal(
            StageChallengeOutcome.StageLocked,
            result.Outcome);
        Assert.Equal(1, state.HighestStage);
    }

    // 성공 시 과거 시간은 기존 배율로 정산하고 이후부터 새 5% 배율을 적용해야 합니다.
    [Fact]
    public void ChallengeStage_OnSuccess_CheckpointsOldRate()
    {
        var createdAt = Utc(0);
        var state = PlayerGameState.Create(
            Guid.NewGuid(),
            createdAt);
        state.ClaimIdleReward(
            createdAt.AddSeconds(100));
        state.UpgradeMainHero(
            createdAt.AddSeconds(100));

        var result = state.ChallengeStage(
            targetStage: 2,
            createdAt.AddSeconds(200));

        Assert.Equal(
            StageChallengeOutcome.Succeeded,
            result.Outcome);
        Assert.Equal(100, result.CheckpointGoldAwarded);
        Assert.Equal(2, state.HighestStage);
        Assert.Equal(5, result.ProductionBonusPercentAfter);
        Assert.Equal(190, state.Gold);
    }

    // 5%의 소수 골드는 짧게 여러 번 받아도 잃지 않고 1/100 단위로 이월되어야 합니다.
    [Fact]
    public void ClaimIdleReward_WithStageBonus_CarriesRemainder()
    {
        var createdAt = Utc(0);
        var state = PlayerGameState.Create(
            Guid.NewGuid(),
            createdAt);
        state.ClaimIdleReward(
            createdAt.AddSeconds(100));
        state.UpgradeMainHero(
            createdAt.AddSeconds(100));
        state.ChallengeStage(
            targetStage: 2,
            createdAt.AddSeconds(100));
        long totalAwarded = 0;

        for (var second = 1; second <= 20; second++)
        {
            var claim = state.ClaimIdleReward(
                createdAt.AddSeconds(100 + second));
            totalAwarded += claim.GoldAwarded;
        }

        Assert.Equal(21, totalAwarded);
        Assert.Equal(0, state.IdleRewardRemainderHundredths);
    }

    private static DateTimeOffset Utc(int hour) =>
        new(2026, 7, 7, hour, 0, 0, TimeSpan.Zero);
}
