namespace IdleGuild.Application.Rewards.ClaimIdleReward;

/// <summary>방치 보상 지급 결과와 멱등 재생 여부를 API에 전달합니다.</summary>
public sealed record ClaimIdleRewardResult(
    string IdempotencyKey,
    long GoldAwarded,
    int AccumulatedSeconds,
    long GoldBalanceAfter,
    int RemainderHundredths,
    int ProductionPercent,
    DateTimeOffset ClaimedAtUtc,
    bool IsReplay);
