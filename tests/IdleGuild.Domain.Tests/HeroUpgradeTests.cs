using IdleGuild.Domain.GameStates;
using IdleGuild.Domain.Heroes;

namespace IdleGuild.Domain.Tests;

/// <summary>영웅 강화 비용, 골드 차감, 실패 시 불변 상태를 검증합니다.</summary>
public sealed class HeroUpgradeTests
{
    // 문서의 15% 복리 비용을 부동소수점 오차 없이 내림 계산해야 합니다.
    [Theory]
    [InlineData(1, 10)]
    [InlineData(2, 11)]
    [InlineData(3, 13)]
    [InlineData(4, 15)]
    [InlineData(100, 10_211_421)]
    [InlineData(296, 8_051_242_439_026_977_036)]
    public void CalculateCost_ReturnsExpectedFloor(
        int currentLevel,
        long expectedCost)
    {
        var cost = HeroUpgradePolicy.CalculateCost(
            currentLevel);

        Assert.Equal(expectedCost, cost);
    }

    // 충분한 골드가 있으면 정확한 비용만 차감하고 레벨을 하나 올려야 합니다.
    [Fact]
    public void UpgradeMainHero_WithEnoughGold_Succeeds()
    {
        var createdAt = Utc(0);
        var state = PlayerGameState.Create(
            Guid.NewGuid(),
            createdAt);
        state.ClaimIdleReward(
            createdAt.AddSeconds(100));

        var result = state.UpgradeMainHero(
            createdAt.AddSeconds(101));

        Assert.Equal(
            HeroUpgradeOutcome.Succeeded,
            result.Outcome);
        Assert.Equal(1, result.PreviousLevel);
        Assert.Equal(2, state.HeroLevel);
        Assert.Equal(10, result.GoldCost);
        Assert.Equal(90, state.Gold);
    }

    // 골드가 부족하면 비용을 알려 주되 레벨과 골드를 변경하지 않아야 합니다.
    [Fact]
    public void UpgradeMainHero_WithInsufficientGold_KeepsState()
    {
        var state = PlayerGameState.Create(
            Guid.NewGuid(),
            Utc(0));

        var result = state.UpgradeMainHero(
            Utc(1));

        Assert.Equal(
            HeroUpgradeOutcome.InsufficientGold,
            result.Outcome);
        Assert.Equal(10, result.GoldCost);
        Assert.Equal(1, state.HeroLevel);
        Assert.Equal(0, state.Gold);
    }

    // long 범위를 넘는 다음 비용이 생기기 전 레벨에서 성장을 명시적으로 제한해야 합니다.
    [Fact]
    public void CalculateCost_AtMaxLevel_Throws()
    {
        var action = () =>
        {
            HeroUpgradePolicy.CalculateCost(
                HeroUpgradePolicy.MaxHeroLevel);
        };

        Assert.Throws<ArgumentOutOfRangeException>(
            action);
    }

    private static DateTimeOffset Utc(int hour) =>
        new(2026, 7, 6, hour, 0, 0, TimeSpan.Zero);
}
