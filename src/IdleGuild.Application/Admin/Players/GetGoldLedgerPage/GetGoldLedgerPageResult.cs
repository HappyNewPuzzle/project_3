using IdleGuild.Application.Abstractions.Persistence;

namespace IdleGuild.Application.Admin.Players.GetGoldLedgerPage;

/// <summary>관리자에게 반환할 골드 원장 항목과 다음 키셋 위치를 묶습니다.</summary>
public sealed record GetGoldLedgerPageResult(
    Guid PlayerId,
    IReadOnlyList<GetGoldLedgerEntryResult> Items,
    GoldLedgerPagePosition? NextPosition);
