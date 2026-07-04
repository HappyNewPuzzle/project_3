using IdleGuild.Domain.GameStates;

namespace IdleGuild.Domain.Tests;

/// <summary>방치 시간 정산의 지급량·상한·시각 보정 규칙을 검증합니다.</summary>
public sealed class IdleRewardTests
{
    // 2시간 동안 기본 초당 1골드를 정확히 지급해야 합니다.
    [Fact]
    public void ClaimIdleReward_AfterTwoHours_AwardsElapsedSeconds()
    {
        var createdAt = Utc(0);
        var state = PlayerGameState.Create(
            Guid.NewGuid(),
            createdAt);

        var result = state.ClaimIdleReward(
            createdAt.AddHours(2));

        Assert.Equal(7_200, result.GoldAwarded);
        Assert.Equal(7_200, result.AccumulatedSeconds);
        Assert.Equal(7_200, state.Gold);
        Assert.Equal(
            createdAt.AddHours(2),
            state.LastIdleRewardClaimedAtUtc);
    }

    // 장기간 접속하지 않아도 한 번에 최대 8시간까지만 누적해야 합니다.
    [Fact]
    public void ClaimIdleReward_AfterTenHours_CapsAtEightHours()
    {
        var createdAt = Utc(0);
        var state = PlayerGameState.Create(
            Guid.NewGuid(),
            createdAt);

        var result = state.ClaimIdleReward(
            createdAt.AddHours(10));

        Assert.Equal(28_800, result.GoldAwarded);
        Assert.Equal(28_800, result.AccumulatedSeconds);
        Assert.Equal(28_800, state.Gold);
    }

    // 같은 시각의 두 번째 정산은 추가 골드를 만들지 않아야 합니다.
    [Fact]
    public void ClaimIdleReward_ImmediatelyAgain_AwardsZero()
    {
        var createdAt = Utc(0);
        var claimAt = createdAt.AddMinutes(10);
        var state = PlayerGameState.Create(
            Guid.NewGuid(),
            createdAt);
        state.ClaimIdleReward(claimAt);

        var result = state.ClaimIdleReward(claimAt);

        Assert.Equal(0, result.GoldAwarded);
        Assert.Equal(600, state.Gold);
    }

    // 서버 시계가 뒤로 가더라도 마지막 정산 시각과 골드는 감소하지 않아야 합니다.
    [Fact]
    public void ClaimIdleReward_WithEarlierTime_DoesNotMoveBackward()
    {
        var createdAt = Utc(0);
        var state = PlayerGameState.Create(
            Guid.NewGuid(),
            createdAt);

        var result = state.ClaimIdleReward(
            createdAt.AddMinutes(-1));

        Assert.Equal(0, result.GoldAwarded);
        Assert.Equal(
            createdAt,
            state.LastIdleRewardClaimedAtUtc);
    }

    private static DateTimeOffset Utc(int hour) =>
        new(2026, 7, 5, hour, 0, 0, TimeSpan.Zero);
}
