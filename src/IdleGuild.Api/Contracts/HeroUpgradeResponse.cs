namespace IdleGuild.Api.Contracts;

/// <summary>영웅 강화 판정, 비용, 처리 후 레벨과 골드를 클라이언트에 전달합니다.</summary>
public sealed record HeroUpgradeResponse(
    string IdempotencyKey,
    string Outcome,
    int PreviousLevel,
    int HeroLevelAfter,
    long GoldCost,
    long GoldBalanceAfter,
    DateTimeOffset ProcessedAtUtc,
    bool IsReplay);
