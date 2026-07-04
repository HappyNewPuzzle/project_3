using IdleGuild.Application.Rewards.ClaimIdleReward;
using IdleGuild.Domain.GameStates;

namespace IdleGuild.Application.Tests;

/// <summary>방치 보상 유스케이스의 지급과 멱등 응답을 검증합니다.</summary>
public sealed class ClaimIdleRewardHandlerTests
{
    // 경과한 초만큼 골드를 지급하고 정산 영수증을 저장해야 합니다.
    [Fact]
    public async Task HandleAsync_WithElapsedTime_AwardsGold()
    {
        var playerId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(
            2026, 7, 5, 0, 0, 0, TimeSpan.Zero);
        var repository =
            new InMemoryPlayerGameStateRepository();
        repository.Add(PlayerGameState.Create(
            playerId,
            createdAt));
        var handler = CreateHandler(
            repository,
            createdAt.AddHours(2));

        var result = await handler.HandleAsync(
            playerId,
            "claim-001");

        Assert.NotNull(result);
        Assert.Equal(7_200, result.GoldAwarded);
        Assert.Equal(7_200, result.AccumulatedSeconds);
        Assert.Equal(7_200, result.GoldBalanceAfter);
        Assert.False(result.IsReplay);
        Assert.Equal(1, repository.SaveCount);
    }

    // 같은 멱등 키를 다시 보내면 저장하거나 골드를 추가하지 않고 최초 결과를 반환해야 합니다.
    [Fact]
    public async Task HandleAsync_WithSameKey_ReplaysFirstResult()
    {
        var playerId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(
            2026, 7, 5, 0, 0, 0, TimeSpan.Zero);
        var repository =
            new InMemoryPlayerGameStateRepository();
        repository.Add(PlayerGameState.Create(
            playerId,
            createdAt));
        var handler = CreateHandler(
            repository,
            createdAt.AddMinutes(30));

        var first = await handler.HandleAsync(
            playerId,
            "retry-safe-key");
        var replay = await handler.HandleAsync(
            playerId,
            "retry-safe-key");

        Assert.NotNull(first);
        Assert.NotNull(replay);
        Assert.False(first.IsReplay);
        Assert.True(replay.IsReplay);
        Assert.Equal(first.GoldAwarded, replay.GoldAwarded);
        Assert.Equal(first.GoldBalanceAfter, replay.GoldBalanceAfter);
        Assert.Equal(1, repository.SaveCount);
    }

    private static ClaimIdleRewardHandler CreateHandler(
        InMemoryPlayerGameStateRepository repository,
        DateTimeOffset now) =>
        new(
            repository,
            repository,
            repository,
            new StubTimeProvider(now));
}
