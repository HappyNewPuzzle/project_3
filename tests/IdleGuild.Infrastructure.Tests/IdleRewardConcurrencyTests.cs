using IdleGuild.Application.Rewards.ClaimIdleReward;
using IdleGuild.Domain.GameStates;
using IdleGuild.Infrastructure.Persistence;
using IdleGuild.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace IdleGuild.Infrastructure.Tests;

/// <summary>동시 보상 요청이 실제 PostgreSQL 제약과 xmin으로 한 번만 지급되는지 검증합니다.</summary>
[Collection(PostgreSqlTestCollection.Name)]
public sealed class IdleRewardConcurrencyTests(
    PostgreSqlDatabaseFixture database)
{
    // 같은 키의 동시 요청 두 개는 하나의 영수증과 하나의 지급 결과만 남겨야 합니다.
    [Fact]
    public async Task SameKeyConcurrentClaims_AwardOnlyOnce()
    {
        var playerId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(
            2026, 7, 5, 0, 0, 0, TimeSpan.Zero);
        var claimAt = createdAt.AddHours(1);

        await using (var seedContext =
                     database.CreateDbContext())
        {
            seedContext.PlayerGameStates.Add(
                PlayerGameState.Create(
                    playerId,
                    createdAt));
            await seedContext.SaveChangesAsync();
        }

        await using var firstContext =
            database.CreateDbContext();
        await using var secondContext =
            database.CreateDbContext();
        var firstHandler = CreateHandler(
            firstContext,
            claimAt);
        var secondHandler = CreateHandler(
            secondContext,
            claimAt);

        var results = await Task.WhenAll(
            firstHandler.HandleAsync(
                playerId,
                "concurrent-key"),
            secondHandler.HandleAsync(
                playerId,
                "concurrent-key"));

        await using var verifyContext =
            database.CreateDbContext();
        var savedState = await verifyContext.PlayerGameStates
            .AsNoTracking()
            .SingleAsync(state =>
                state.PlayerId == playerId);
        var receiptCount =
            await verifyContext.IdleRewardClaimReceipts
                .CountAsync(receipt =>
                    receipt.PlayerId == playerId);

        Assert.All(results, result =>
        {
            Assert.NotNull(result);
            Assert.Equal(3_600, result.GoldAwarded);
            Assert.Equal(3_600, result.GoldBalanceAfter);
        });
        Assert.Contains(results, result =>
            result!.IsReplay);
        Assert.Equal(3_600, savedState.Gold);
        Assert.Equal(1, receiptCount);
    }

    private static ClaimIdleRewardHandler CreateHandler(
        GameDbContext context,
        DateTimeOffset now) =>
        new(
            new PlayerGameStateRepository(context),
            new IdleRewardClaimRepository(context),
            new EfGameUnitOfWork(context),
            new FixedTimeProvider(now));

    /// <summary>두 동시 요청이 같은 서버 시각을 사용하게 고정합니다.</summary>
    private sealed class FixedTimeProvider(
        DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() =>
            now;
    }
}
