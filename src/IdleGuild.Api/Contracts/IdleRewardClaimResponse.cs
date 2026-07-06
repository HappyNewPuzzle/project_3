namespace IdleGuild.Api.Contracts;

/// <summary>방치 보상 지급량과 지급 후 잔액을 클라이언트에 전달합니다.</summary>
public sealed record IdleRewardClaimResponse(
    string IdempotencyKey,
    long GoldAwarded,
    int AccumulatedSeconds,
    long GoldBalanceAfter,
    int RemainderHundredths,
    int ProductionPercent,
    DateTimeOffset ClaimedAtUtc,
    bool IsReplay);
