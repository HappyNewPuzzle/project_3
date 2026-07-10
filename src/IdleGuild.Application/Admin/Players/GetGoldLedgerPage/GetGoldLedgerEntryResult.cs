using IdleGuild.Domain.Economy;

namespace IdleGuild.Application.Admin.Players.GetGoldLedgerPage;

/// <summary>관리자 원장 페이지에 표시할 한 번의 골드 변경을 표현합니다.</summary>
public sealed record GetGoldLedgerEntryResult(
    Guid EntryId,
    GoldLedgerReason Reason,
    long BalanceBefore,
    long Amount,
    long BalanceAfter,
    string ReferenceId,
    DateTimeOffset OccurredAtUtc);
