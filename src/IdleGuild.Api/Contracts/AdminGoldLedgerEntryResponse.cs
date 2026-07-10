namespace IdleGuild.Api.Contracts;

/// <summary>관리자에게 노출할 한 번의 골드 변경 감사 계약입니다.</summary>
public sealed record AdminGoldLedgerEntryResponse(
    Guid EntryId,
    string Reason,
    long BalanceBefore,
    long Amount,
    long BalanceAfter,
    string ReferenceId,
    DateTimeOffset OccurredAtUtc);
