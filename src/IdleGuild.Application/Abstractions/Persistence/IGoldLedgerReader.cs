using IdleGuild.Domain.Economy;

namespace IdleGuild.Application.Abstractions.Persistence;

/// <summary>관리자 조회를 위한 플레이어별 골드 원장 읽기를 저장 기술과 분리합니다.</summary>
public interface IGoldLedgerReader
{
    Task<IReadOnlyList<GoldLedgerEntry>> ListByPlayerAsync(
        Guid playerId,
        int take,
        GoldLedgerPagePosition? before,
        CancellationToken cancellationToken = default);
}
