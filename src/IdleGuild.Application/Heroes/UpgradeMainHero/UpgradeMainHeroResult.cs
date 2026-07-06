using IdleGuild.Domain.Heroes;

namespace IdleGuild.Application.Heroes.UpgradeMainHero;

/// <summary>영웅 강화 판정과 멱등 재생 여부를 API에 전달합니다.</summary>
public sealed record UpgradeMainHeroResult(
    string IdempotencyKey,
    HeroUpgradeOutcome Outcome,
    int PreviousLevel,
    int HeroLevelAfter,
    long GoldCost,
    long GoldBalanceAfter,
    DateTimeOffset ProcessedAtUtc,
    bool IsReplay);
