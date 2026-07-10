using IdleGuild.Application.Abstractions.Persistence;

namespace IdleGuild.Application.Admin.Players.GetGoldLedgerPage;

/// <summary>플레이어 존재를 확인한 뒤 최신순 골드 원장을 키셋 방식으로 조회합니다.</summary>
public sealed class GetGoldLedgerPageHandler(
    IPlayerGameStateRepository gameStateRepository,
    IGoldLedgerReader goldLedgerReader)
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 100;

    /// <summary>요청 크기보다 한 행 더 읽어 다음 페이지 존재 여부를 계산합니다.</summary>
    public async Task<GetGoldLedgerPageResult?> HandleAsync(
        Guid playerId,
        int pageSize,
        GoldLedgerPagePosition? before,
        CancellationToken cancellationToken = default)
    {
        if (playerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Player ID must not be empty.",
                nameof(playerId));
        }

        if (pageSize < 1 || pageSize > MaxPageSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageSize),
                $"Page size must be between 1 and {MaxPageSize}.");
        }

        if (before is not null &&
            (before.OccurredAtUtc == default ||
             before.EntryId == Guid.Empty))
        {
            throw new ArgumentException(
                "Ledger page position is invalid.",
                nameof(before));
        }

        var state = await gameStateRepository.FindByIdAsync(
            playerId,
            cancellationToken);

        if (state is null)
        {
            return null;
        }

        var entries = await goldLedgerReader
            .ListByPlayerAsync(
                playerId,
                pageSize + 1,
                before,
                cancellationToken);
        var pageItems = entries
            .Take(pageSize)
            .Select(entry =>
                new GetGoldLedgerEntryResult(
                    entry.EntryId,
                    entry.Reason,
                    entry.BalanceBefore,
                    entry.Amount,
                    entry.BalanceAfter,
                    entry.ReferenceId,
                    entry.OccurredAtUtc))
            .ToArray();
        var lastItem = pageItems.LastOrDefault();
        var nextPosition = entries.Count > pageSize &&
            lastItem is not null
            ? new GoldLedgerPagePosition(
                lastItem.OccurredAtUtc,
                lastItem.EntryId)
            : null;

        return new GetGoldLedgerPageResult(
            playerId,
            pageItems,
            nextPosition);
    }
}
