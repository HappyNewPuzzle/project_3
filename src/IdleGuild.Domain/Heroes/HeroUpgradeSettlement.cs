namespace IdleGuild.Domain.Heroes;

/// <summary>한 번의 영웅 강화 판정과 판정 직후 상태를 전달합니다.</summary>
public sealed record HeroUpgradeSettlement(
    HeroUpgradeOutcome Outcome,
    int PreviousLevel,
    int HeroLevelAfter,
    long GoldCost,
    long GoldBalanceAfter,
    DateTimeOffset ProcessedAtUtc);
