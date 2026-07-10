namespace IdleGuild.Application.Abstractions.Persistence;

/// <summary>골드 원장 키셋 페이지의 마지막 시각과 행 ID를 표현합니다.</summary>
public sealed record GoldLedgerPagePosition(
    DateTimeOffset OccurredAtUtc,
    Guid EntryId);
