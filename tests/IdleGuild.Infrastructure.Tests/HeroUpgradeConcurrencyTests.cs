using IdleGuild.Application.Abstractions.Persistence;
using IdleGuild.Application.Heroes.UpgradeMainHero;
using IdleGuild.Domain.GameStates;
using IdleGuild.Domain.Heroes;
using IdleGuild.Infrastructure.Persistence;
using IdleGuild.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace IdleGuild.Infrastructure.Tests;

/// <summary>동시 강화가 실제 PostgreSQL xmin과 최신 잔액 재판정으로 안전한지 검증합니다.</summary>
[Collection(PostgreSqlTestCollection.Name)]
public sealed class HeroUpgradeConcurrencyTests(
    PostgreSqlDatabaseFixture database)
{
    // 골드 10에 서로 다른 요청 두 개가 동시에 와도 한 번만 강화되어야 합니다.
    [Fact]
    public async Task DifferentConcurrentUpgrades_WithOneCost_OnlyOneSucceeds()
    {
        var playerId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(
            2026, 7, 6, 0, 0, 0, TimeSpan.Zero);
        var state = PlayerGameState.Create(
            playerId,
            createdAt);
        state.ClaimIdleReward(
            createdAt.AddSeconds(10));

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
            createdAt.AddMinutes(1));
        var secondHandler = CreateHandler(
            secondContext,
            readGate,
            createdAt.AddMinutes(1));

        var results = await Task.WhenAll(
            firstHandler.HandleAsync(
                playerId,
                "upgrade-concurrent-a"),
            secondHandler.HandleAsync(
                playerId,
                "upgrade-concurrent-b"));

        await using var verifyContext =
            database.CreateDbContext();
        var savedState = await verifyContext.PlayerGameStates
            .AsNoTracking()
            .SingleAsync(saved =>
                saved.PlayerId == playerId);
        var receipts = await verifyContext.HeroUpgradeReceipts
            .AsNoTracking()
            .Where(receipt =>
                receipt.PlayerId == playerId)
            .ToArrayAsync();
        var ledgerEntries = await verifyContext
            .GoldLedgerEntries
            .AsNoTracking()
            .Where(entry => entry.PlayerId == playerId)
            .ToArrayAsync();

        Assert.Single(results, result =>
            result!.Outcome ==
            HeroUpgradeOutcome.Succeeded);
        Assert.Single(results, result =>
            result!.Outcome ==
            HeroUpgradeOutcome.InsufficientGold);
        Assert.Equal(2, savedState.HeroLevel);
        Assert.Equal(0, savedState.Gold);
        Assert.Equal(2, receipts.Length);
        var ledger = Assert.Single(ledgerEntries);
        Assert.Equal(-10, ledger.Amount);
    }

    // 상태를 바꾸지 않는 골드 부족 요청도 같은 키라면 영수증 하나만 저장해야 합니다.
    [Fact]
    public async Task SameConcurrentFailureKey_CreatesOneReceipt()
    {
        var playerId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(
            2026, 7, 6, 0, 0, 0, TimeSpan.Zero);

        await using (var seedContext =
                     database.CreateDbContext())
        {
            seedContext.PlayerGameStates.Add(
                PlayerGameState.Create(
                    playerId,
                    createdAt));
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
            createdAt.AddMinutes(1));
        var secondHandler = CreateHandler(
            secondContext,
            readGate,
            createdAt.AddMinutes(1));

        var results = await Task.WhenAll(
            firstHandler.HandleAsync(
                playerId,
                "same-failure-key"),
            secondHandler.HandleAsync(
                playerId,
                "same-failure-key"));

        await using var verifyContext =
            database.CreateDbContext();
        var receiptCount =
            await verifyContext.HeroUpgradeReceipts
                .CountAsync(receipt =>
                    receipt.PlayerId == playerId);
        var ledgerCount =
            await verifyContext.GoldLedgerEntries
                .CountAsync(entry =>
                    entry.PlayerId == playerId);

        Assert.All(results, result =>
        {
            Assert.NotNull(result);
            Assert.Equal(
                HeroUpgradeOutcome.InsufficientGold,
                result.Outcome);
        });
        Assert.Contains(results, result =>
            result!.IsReplay);
        Assert.Equal(1, receiptCount);
        Assert.Equal(0, ledgerCount);
    }

    private static UpgradeMainHeroHandler CreateHandler(
        GameDbContext context,
        FirstTwoReadsGate readGate,
        DateTimeOffset now) =>
        new(
            new GatedPlayerGameStateRepository(
                new PlayerGameStateRepository(context),
                readGate),
            new HeroUpgradeReceiptRepository(context),
            new GoldLedgerRepository(context),
            new EfGameUnitOfWork(context),
            new FixedTimeProvider(now));

    /// <summary>최초 두 상태 조회가 모두 끝난 뒤 동시에 계산을 시작하게 합니다.</summary>
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

    /// <summary>실제 저장소 조회 뒤 테스트 동시성 시작점만 맞춥니다.</summary>
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

    /// <summary>두 Handler가 동일한 서버 처리 시각을 사용하게 합니다.</summary>
    private sealed class FixedTimeProvider(
        DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() =>
            now;
    }
}
