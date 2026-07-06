using IdleGuild.Application.Abstractions.Persistence;
using IdleGuild.Application.Stages.ChallengeStage;
using IdleGuild.Domain.GameStates;
using IdleGuild.Domain.Stages;
using IdleGuild.Infrastructure.Persistence;
using IdleGuild.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace IdleGuild.Infrastructure.Tests;

/// <summary>스테이지 동시 진행과 생산 소수 잔여값을 실제 PostgreSQL에서 검증합니다.</summary>
[Collection(PostgreSqlTestCollection.Name)]
public sealed class StageProgressionPersistenceTests(
    PostgreSqlDatabaseFixture database)
{
    // 같은 다음 스테이지의 서로 다른 동시 요청은 하나만 진행 상태를 바꿔야 합니다.
    [Fact]
    public async Task ConcurrentNextStageChallenges_ProgressOnce()
    {
        var playerId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(
            2026, 7, 7, 0, 0, 0, TimeSpan.Zero);
        var state = CreateLevelTwoState(
            playerId,
            createdAt);

        await using (var seedContext =
                     database.CreateDbContext())
        {
            seedContext.PlayerGameStates.Add(state);
            await seedContext.SaveChangesAsync();
        }

        var readGate = new FirstTwoReadsGate();
        await using var firstContext =
            database.CreateDbContext();
        await using var secondContext =
            database.CreateDbContext();
        var firstHandler = CreateHandler(
            firstContext,
            readGate,
            createdAt.AddSeconds(200));
        var secondHandler = CreateHandler(
            secondContext,
            readGate,
            createdAt.AddSeconds(200));

        var results = await Task.WhenAll(
            firstHandler.HandleAsync(
                playerId,
                targetStage: 2,
                "stage-concurrent-a"),
            secondHandler.HandleAsync(
                playerId,
                targetStage: 2,
                "stage-concurrent-b"));

        await using var verifyContext =
            database.CreateDbContext();
        var savedState = await verifyContext.PlayerGameStates
            .AsNoTracking()
            .SingleAsync(saved =>
                saved.PlayerId == playerId);
        var receipts = await verifyContext
            .StageChallengeReceipts
            .AsNoTracking()
            .Where(receipt =>
                receipt.PlayerId == playerId)
            .ToArrayAsync();

        Assert.Single(results, result =>
            result!.Outcome ==
            StageChallengeOutcome.Succeeded);
        Assert.Single(results, result =>
            result!.Outcome ==
            StageChallengeOutcome.AlreadyCompleted);
        Assert.Equal(2, savedState.HighestStage);
        Assert.Equal(190, savedState.Gold);
        Assert.Equal(2, receipts.Length);
    }

    // 1/100 골드 잔여값은 DbContext가 바뀌어도 보존되어야 합니다.
    [Fact]
    public async Task RewardRemainder_RoundTripsThroughPostgreSql()
    {
        var playerId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(
            2026, 7, 7, 0, 0, 0, TimeSpan.Zero);
        var state = CreateLevelTwoState(
            playerId,
            createdAt);
        state.ChallengeStage(
            targetStage: 2,
            createdAt.AddSeconds(100));
        var claim = state.ClaimIdleReward(
            createdAt.AddSeconds(101));

        await using (var writeContext =
                     database.CreateDbContext())
        {
            writeContext.PlayerGameStates.Add(state);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext =
            database.CreateDbContext();
        var saved = await readContext.PlayerGameStates
            .AsNoTracking()
            .SingleAsync(candidate =>
                candidate.PlayerId == playerId);

        Assert.Equal(1, claim.GoldAwarded);
        Assert.Equal(5, claim.RemainderHundredths);
        Assert.Equal(5, saved.IdleRewardRemainderHundredths);
        Assert.Equal(2, saved.HighestStage);
    }

    private static PlayerGameState CreateLevelTwoState(
        Guid playerId,
        DateTimeOffset createdAt)
    {
        var state = PlayerGameState.Create(
            playerId,
            createdAt);
        state.ClaimIdleReward(
            createdAt.AddSeconds(100));
        state.UpgradeMainHero(
            createdAt.AddSeconds(100));
        return state;
    }

    private static ChallengeStageHandler CreateHandler(
        GameDbContext context,
        FirstTwoReadsGate readGate,
        DateTimeOffset now) =>
        new(
            new GatedPlayerGameStateRepository(
                new PlayerGameStateRepository(context),
                readGate),
            new StageChallengeReceiptRepository(context),
            new EfGameUnitOfWork(context),
            new FixedTimeProvider(now));

    /// <summary>최초 두 상태 조회가 모두 끝난 뒤 동시에 판정하게 합니다.</summary>
    private sealed class FirstTwoReadsGate
    {
        private readonly TaskCompletionSource _bothRead =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _readCount;

        public async Task WaitAsync()
        {
            var readNumber = Interlocked.Increment(
                ref _readCount);

            if (readNumber == 2)
            {
                _bothRead.SetResult();
            }

            if (readNumber <= 2)
            {
                await _bothRead.Task;
            }
        }
    }

    /// <summary>실제 상태 조회가 끝난 시점에만 테스트 동시성 시작점을 맞춥니다.</summary>
    private sealed class GatedPlayerGameStateRepository(
        PlayerGameStateRepository inner,
        FirstTwoReadsGate readGate) :
        IPlayerGameStateRepository
    {
        public void Add(PlayerGameState gameState) =>
            inner.Add(gameState);

        public Task<PlayerGameState?> FindByIdAsync(
            Guid playerId,
            CancellationToken cancellationToken = default) =>
            inner.FindByIdAsync(
                playerId,
                cancellationToken);

        public async Task<PlayerGameState?> FindForUpdateAsync(
            Guid playerId,
            CancellationToken cancellationToken = default)
        {
            var state = await inner.FindForUpdateAsync(
                playerId,
                cancellationToken);
            await readGate.WaitAsync();
            return state;
        }
    }

    /// <summary>동시 Handler가 같은 서버 처리 시각을 사용하게 합니다.</summary>
    private sealed class FixedTimeProvider(
        DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() =>
            now;
    }
}
