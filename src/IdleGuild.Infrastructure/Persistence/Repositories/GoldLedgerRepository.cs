using IdleGuild.Application.Abstractions.Persistence;
using IdleGuild.Domain.Economy;

namespace IdleGuild.Infrastructure.Persistence.Repositories;

/// <summary>골드 변경 원장을 EF Core 작업 단위에 추가합니다.</summary>
public sealed class GoldLedgerRepository(
    GameDbContext dbContext) : IGoldLedgerRepository
{
    /// <summary>검증된 원장 행을 현재 작업 단위의 변경 추적기에 추가합니다.</summary>
    public void Add(GoldLedgerEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        dbContext.GoldLedgerEntries.Add(entry);
    }
}
