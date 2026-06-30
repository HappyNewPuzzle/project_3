using IdleGuild.Domain.GameStates;
using Microsoft.EntityFrameworkCore;

namespace IdleGuild.Infrastructure.Tests;

/// <summary>게임 상태가 실제 PostgreSQL 스키마에 저장되고 복원되는지 검증합니다.</summary>
public sealed class PlayerGameStatePersistenceTests(
    PostgreSqlDatabaseFixture database) :
    IClassFixture<PostgreSqlDatabaseFixture>
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
            writeContext.PlayerGameStates.Add(state);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = database.CreateDbContext();
        var saved = await readContext.PlayerGameStates
            .AsNoTracking()
            .SingleAsync(item => item.PlayerId == playerId);

        Assert.Equal(0, saved.Gold);
        Assert.Equal(1, saved.HeroLevel);
        Assert.Equal(1, saved.HighestStage);
        Assert.Equal(createdAtUtc, saved.CreatedAtUtc);
        Assert.Equal(createdAtUtc, saved.LastIdleRewardClaimedAtUtc);
        Assert.NotEqual(0u, saved.Version);
    }
}
