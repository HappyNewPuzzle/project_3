namespace IdleGuild.Domain.Rewards;

/// <summary>한 번의 방치 보상 계산 결과를 표현합니다.</summary>
public sealed record IdleRewardSettlement(
    long GoldAwarded,
    int AccumulatedSeconds,
    long GoldBalanceAfter,
    DateTimeOffset ClaimedAtUtc);
