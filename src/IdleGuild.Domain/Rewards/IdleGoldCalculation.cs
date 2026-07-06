namespace IdleGuild.Domain.Rewards;

/// <summary>방치 골드의 정수 지급량, 소수 잔여값, 적용 배율을 전달합니다.</summary>
public sealed record IdleGoldCalculation(
    long GoldAwarded,
    int RemainderHundredths,
    int ProductionPercent);
