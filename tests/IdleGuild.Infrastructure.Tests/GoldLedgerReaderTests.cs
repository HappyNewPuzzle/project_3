using IdleGuild.Application.Abstractions.Persistence;
using IdleGuild.Domain.Economy;
using IdleGuild.Domain.GameStates;
using IdleGuild.Infrastructure.Persistence.Repositories;

namespace IdleGuild.Infrastructure.Tests;

/// <summary>관리자 골드 원장 키셋 조회가 실제 PostgreSQL 정렬과 필터를 사용하는지 검증합니다.</summary>
[Collection(PostgreSqlTestCollection.Name)]
public sealed class GoldLedgerReaderTests(
    PostgreSqlDatabaseFixture database)
{
    // 첫 페이지의 마지막 위치 뒤에는 같은 플레이어의 남은 과거 행만 반환해야 합니다.
    [Fact]
    public async Task ListByPlayer_WithPosition_ReturnsOlderRowsOnly()
    {
        var playerId = Guid.NewGuid();
        var otherPlayerId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(
            2026, 7, 11, 0, 0, 0, TimeSpan.Zero);

        await using (var seedContext =
                     database.CreateDbContext())
        {
            seedContext.PlayerGameStates.AddRange(
                PlayerGameState.Create(
                    playerId,
                    createdAt),
                PlayerGameState.Create(
                    otherPlayerId,
                    createdAt));
            seedContext.GoldLedgerEntries.AddRange(
                CreateEntry(playerId, "entry-1", 1),
                CreateEntry(playerId, "entry-2", 2),
                CreateEntry(playerId, "entry-3", 3),
                CreateEntry(otherPlayerId, "other", 4));
            await seedContext.SaveChangesAsync();
        }

        await using var readContext =
            database.CreateDbContext();
        var reader = new GoldLedgerReader(readContext);
        var first = await reader.ListByPlayerAsync(
            playerId,
            take: 2,
            before: null);
        var position = new GoldLedgerPagePosition(
            first[^1].OccurredAtUtc,
            first[^1].EntryId);
        var second = await reader.ListByPlayerAsync(
            playerId,
            take: 2,
            position);

        Assert.Equal(
            ["entry-3", "entry-2"],
            first.Select(entry => entry.ReferenceId));
        var remaining = Assert.Single(second);
        Assert.Equal("entry-1", remaining.ReferenceId);
    }

    private static GoldLedgerEntry CreateEntry(
        Guid playerId,
        string referenceId,
        int hour) =>
        GoldLedgerEntry.Create(
            playerId,
            GoldLedgerReason.IdleRewardClaim,
            balanceBefore: 0,
            amount: 1,
            balanceAfter: 1,
            referenceId,
            new DateTimeOffset(
                2026,
                7,
                11,
                hour,
                0,
                0,
                TimeSpan.Zero));
}
