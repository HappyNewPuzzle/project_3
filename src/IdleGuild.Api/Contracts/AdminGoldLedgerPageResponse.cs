namespace IdleGuild.Api.Contracts;

/// <summary>최신순 골드 원장 항목과 다음 페이지 커서를 반환합니다.</summary>
public sealed record AdminGoldLedgerPageResponse(
    Guid PlayerId,
    IReadOnlyList<AdminGoldLedgerEntryResponse> Items,
    string? NextCursor);
