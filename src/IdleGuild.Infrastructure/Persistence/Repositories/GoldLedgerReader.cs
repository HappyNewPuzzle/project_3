using IdleGuild.Application.Abstractions.Persistence;
using IdleGuild.Domain.Economy;
using Microsoft.EntityFrameworkCore;

namespace IdleGuild.Infrastructure.Persistence.Repositories;

/// <summary>플레이어별 골드 원장을 PostgreSQL에서 최신순 키셋 페이지로 조회합니다.</summary>
public sealed class GoldLedgerReader(
    GameDbContext dbContext) : IGoldLedgerReader
{
    public async Task<IReadOnlyList<GoldLedgerEntry>>
        ListByPlayerAsync(
            Guid playerId,
            int take,
            GoldLedgerPagePosition? before,
            CancellationToken cancellationToken = default)
    {
        var query = dbContext.GoldLedgerEntries
            .AsNoTracking()
            .Where(entry => entry.PlayerId == playerId);

        if (before is not null)
        {
            var beforeUtc = before.OccurredAtUtc
                .ToUniversalTime();
            query = query.Where(entry =>
                entry.OccurredAtUtc < beforeUtc ||
                (entry.OccurredAtUtc == beforeUtc &&
                 entry.EntryId.CompareTo(before.EntryId) < 0));
        }

        return await query
            .OrderByDescending(entry => entry.OccurredAtUtc)
            .ThenByDescending(entry => entry.EntryId)
            .Take(take)
            .ToArrayAsync(cancellationToken);
    }
}
