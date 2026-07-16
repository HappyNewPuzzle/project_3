using IdleGuild.Application.Profiles.UpdateSelectedHero;
using IdleGuild.Application.Rewards.PreviewIdleReward;
using IdleGuild.Domain.GameStates;
using IdleGuild.Infrastructure.Persistence;
using IdleGuild.Infrastructure.Persistence.Repositories;

namespace IdleGuild.Infrastructure.Tests;

/// <summary>게임 상태가 실제 PostgreSQL 스키마에 저장되고 복원되는지 검증합니다.</summary>
[Collection(PostgreSqlTestCollection.Name)]
public sealed class PlayerGameStatePersistenceTests(
    PostgreSqlDatabaseFixture database)
{
    // 저장과 조회에 서로 다른 DbContext를 사용해 메모리 캐시가 아닌 DB 결과를 검증합니다.
    [Fact]
    public async Task PlayerGameState_RoundTripsThroughPostgreSql()
    {
        var playerId = Guid.NewGuid();
        var createdAtUtc = new DateTimeOffset(
            2026, 6, 30, 1, 2, 3, TimeSpan.Zero);
        var state = PlayerGameState.Create(
            playerId,
            createdAtUtc);

        await using (var writeContext = database.CreateDbContext())
        {
            var writeRepository =
                new PlayerGameStateRepository(writeContext);
            writeRepository.Add(state);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = database.CreateDbContext();
        var readRepository =
            new PlayerGameStateRepository(readContext);
        var saved = await readRepository.FindByIdAsync(playerId);

        Assert.NotNull(saved);
        Assert.Equal(0, saved.Gold);
        Assert.Equal(1, saved.HeroLevel);
        Assert.Equal(1, saved.HighestStage);
        Assert.Equal(SelectedHeroPolicy.DefaultHeroId, saved.SelectedHeroId);
        Assert.Equal(createdAtUtc, saved.CreatedAtUtc);
        Assert.Equal(createdAtUtc, saved.LastIdleRewardClaimedAtUtc);
        Assert.Equal(0, saved.IdleRewardRemainderHundredths);
        Assert.NotEqual(0u, saved.Version);
    }

    [Fact]
    public async Task SelectedHero_RoundTripsThroughPostgreSql()
    {
        var playerId = Guid.NewGuid();
        var createdAtUtc = new DateTimeOffset(
            2026, 7, 17, 0, 0, 0, TimeSpan.Zero);

        await using (var context = database.CreateDbContext())
        {
            var repository = new PlayerGameStateRepository(context);
            repository.Add(PlayerGameState.Create(playerId, createdAtUtc));
            await context.SaveChangesAsync();

            var handler = new UpdateSelectedHeroHandler(
                repository,
                new EfGameUnitOfWork(context));
            var selectedHeroId = await handler.HandleAsync(
                playerId,
                SelectedHeroPolicy.BlackCatHeroId);

            Assert.Equal(SelectedHeroPolicy.BlackCatHeroId, selectedHeroId);
        }

        await using var readContext = database.CreateDbContext();
        var saved = await new PlayerGameStateRepository(readContext)
            .FindByIdAsync(playerId);
        Assert.Equal(SelectedHeroPolicy.BlackCatHeroId, saved!.SelectedHeroId);
    }

    [Fact]
    public async Task IdleRewardPreview_DoesNotChangePostgreSqlState()
    {
        var playerId = Guid.NewGuid();
        var createdAtUtc = new DateTimeOffset(
            2026, 7, 17, 0, 0, 0, TimeSpan.Zero);

        await using (var writeContext = database.CreateDbContext())
        {
            writeContext.PlayerGameStates.Add(
                PlayerGameState.Create(playerId, createdAtUtc));
            await writeContext.SaveChangesAsync();
        }

        await using (var previewContext = database.CreateDbContext())
        {
            var handler = new PreviewIdleRewardHandler(
                new PlayerGameStateRepository(previewContext),
                new FixedTimeProvider(createdAtUtc.AddHours(1)));
            var preview = await handler.HandleAsync(playerId);

            Assert.NotNull(preview);
            Assert.Equal(3_600, preview.ElapsedSeconds);
            Assert.Equal(3_600, preview.ClaimableGold);
        }

        await using var readContext = database.CreateDbContext();
        var saved = await new PlayerGameStateRepository(readContext)
            .FindByIdAsync(playerId);
        Assert.NotNull(saved);
        Assert.Equal(0, saved.Gold);
        Assert.Equal(createdAtUtc, saved.LastIdleRewardClaimedAtUtc);
        Assert.Equal(0, saved.IdleRewardRemainderHundredths);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
