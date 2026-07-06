using IdleGuild.Application.Heroes.UpgradeMainHero;
using IdleGuild.Domain.GameStates;
using IdleGuild.Domain.Heroes;

namespace IdleGuild.Application.Tests;

/// <summary>영웅 강화 유스케이스의 성공·실패·멱등 응답을 검증합니다.</summary>
public sealed class UpgradeMainHeroHandlerTests
{
    // 충분한 골드는 한 번만 차감되고 같은 키는 최초 성공 결과를 재생해야 합니다.
    [Fact]
    public async Task HandleAsync_WithSameKey_ReplaysSuccess()
    {
        var playerId = Guid.NewGuid();
        var createdAt = Utc(0);
        var repository =
            new InMemoryPlayerGameStateRepository();
        var state = PlayerGameState.Create(
            playerId,
            createdAt);
        state.ClaimIdleReward(
            createdAt.AddSeconds(100));
        repository.Add(state);
        var handler = CreateHandler(
            repository,
            createdAt.AddMinutes(2));

        var first = await handler.HandleAsync(
            playerId,
            "upgrade-success");
        var replay = await handler.HandleAsync(
            playerId,
            "upgrade-success");

        Assert.NotNull(first);
        Assert.NotNull(replay);
        Assert.Equal(
            HeroUpgradeOutcome.Succeeded,
            first.Outcome);
        Assert.False(first.IsReplay);
        Assert.True(replay.IsReplay);
        Assert.Equal(2, state.HeroLevel);
        Assert.Equal(90, state.Gold);
        Assert.Equal(1, repository.SaveCount);
    }

    // 실패한 키는 이후 골드가 생겨도 실패로 재생하고 새 키만 새 상태로 판정해야 합니다.
    [Fact]
    public async Task HandleAsync_ReplayedFailure_DoesNotBecomeSuccess()
    {
        var playerId = Guid.NewGuid();
        var createdAt = Utc(0);
        var repository =
            new InMemoryPlayerGameStateRepository();
        var state = PlayerGameState.Create(
            playerId,
            createdAt);
        repository.Add(state);
        var handler = CreateHandler(
            repository,
            createdAt.AddMinutes(1));

        var failed = await handler.HandleAsync(
            playerId,
            "upgrade-failed");
        state.ClaimIdleReward(
            createdAt.AddSeconds(100));
        var replay = await handler.HandleAsync(
            playerId,
            "upgrade-failed");
        var newRequest = await handler.HandleAsync(
            playerId,
            "upgrade-new");

        Assert.NotNull(failed);
        Assert.NotNull(replay);
        Assert.NotNull(newRequest);
        Assert.Equal(
            HeroUpgradeOutcome.InsufficientGold,
            replay.Outcome);
        Assert.True(replay.IsReplay);
        Assert.Equal(
            HeroUpgradeOutcome.Succeeded,
            newRequest.Outcome);
        Assert.Equal(2, state.HeroLevel);
        Assert.Equal(2, repository.SaveCount);
    }

    private static UpgradeMainHeroHandler CreateHandler(
        InMemoryPlayerGameStateRepository repository,
        DateTimeOffset now) =>
        new(
            repository,
            repository,
            repository,
            new StubTimeProvider(now));

    private static DateTimeOffset Utc(int hour) =>
        new(2026, 7, 6, hour, 0, 0, TimeSpan.Zero);
}
