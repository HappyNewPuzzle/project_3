using IdleGuild.Application.Abstractions.Persistence;
using IdleGuild.Application.Stages.ChallengeStage;
using IdleGuild.Domain.GameStates;
using IdleGuild.Domain.Stages;

namespace IdleGuild.Application.Tests;

/// <summary>스테이지 유스케이스의 판정 저장, 재생, 키 충돌을 검증합니다.</summary>
public sealed class ChallengeStageHandlerTests
{
    // 실패 판정도 같은 키에서는 다시 계산하지 않고 최초 결과를 반환해야 합니다.
    [Fact]
    public async Task HandleAsync_WithSameKey_ReplaysFailure()
    {
        var playerId = Guid.NewGuid();
        var repository =
            new InMemoryPlayerGameStateRepository();
        repository.Add(PlayerGameState.Create(
            playerId,
            Utc(0)));
        var handler = CreateHandler(
            repository,
            Utc(1));

        var first = await handler.HandleAsync(
            playerId,
            targetStage: 2,
            "stage-failure");
        var replay = await handler.HandleAsync(
            playerId,
            targetStage: 2,
            "stage-failure");

        Assert.NotNull(first);
        Assert.NotNull(replay);
        Assert.Equal(
            StageChallengeOutcome.InsufficientPower,
            first.Outcome);
        Assert.False(first.IsReplay);
        Assert.True(replay.IsReplay);
        Assert.Equal(1, repository.SaveCount);
    }

    // 하나의 키를 다른 목표 스테이지에 재사용하면 모호한 재생 대신 충돌을 알려야 합니다.
    [Fact]
    public async Task HandleAsync_SameKeyDifferentStage_Throws()
    {
        var playerId = Guid.NewGuid();
        var repository =
            new InMemoryPlayerGameStateRepository();
        repository.Add(PlayerGameState.Create(
            playerId,
            Utc(0)));
        var handler = CreateHandler(
            repository,
            Utc(1));
        await handler.HandleAsync(
            playerId,
            targetStage: 2,
            "reused-key");

        var action = () => handler.HandleAsync(
            playerId,
            targetStage: 3,
            "reused-key");

        await Assert.ThrowsAsync<
            IdempotencyKeyConflictException>(action);
    }

    // 충분한 전투력은 다음 스테이지를 열고 같은 키 재요청에서 성공을 재생해야 합니다.
    [Fact]
    public async Task HandleAsync_WithEnoughPower_UnlocksStage()
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
        state.UpgradeMainHero(
            createdAt.AddSeconds(100));
        repository.Add(state);
        var handler = CreateHandler(
            repository,
            createdAt.AddSeconds(200));

        var result = await handler.HandleAsync(
            playerId,
            targetStage: 2,
            "stage-success");

        Assert.NotNull(result);
        Assert.Equal(
            StageChallengeOutcome.Succeeded,
            result.Outcome);
        Assert.Equal(2, state.HighestStage);
        Assert.Equal(5, result.ProductionBonusPercentAfter);
        Assert.Equal(100, result.CheckpointGoldAwarded);
        Assert.Equal(1, repository.SaveCount);
    }

    private static ChallengeStageHandler CreateHandler(
        InMemoryPlayerGameStateRepository repository,
        DateTimeOffset now) =>
        new(
            repository,
            repository,
            repository,
            new StubTimeProvider(now));

    private static DateTimeOffset Utc(int hour) =>
        new(2026, 7, 7, hour, 0, 0, TimeSpan.Zero);
}
