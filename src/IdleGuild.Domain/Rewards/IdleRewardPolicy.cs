namespace IdleGuild.Domain.Rewards;

/// <summary>MVP 방치 보상의 서버 권위형 고정 규칙을 정의합니다.</summary>
public static class IdleRewardPolicy
{
    public const int BaseGoldPerSecond = 1;

    public const int MaxAccumulationSeconds = 8 * 60 * 60;

    public const int PercentScale = 100;

    public const int StageBonusPercent = 5;

    /// <summary>최고 스테지에 따른 기준 대비 생산 배율을 백분율로 계산합니다.</summary>
    public static int CalculateProductionPercent(
        int highestStage)
    {
        if (highestStage < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(highestStage));
        }

        return checked(
            PercentScale +
            ((highestStage - 1) * StageBonusPercent));
    }

    /// <summary>전체 초와 이전 소수 잔여값을 합쳐 지급 골드와 새 잔여값을 계산합니다.</summary>
    public static IdleGoldCalculation CalculateGold(
        int accumulatedSeconds,
        int highestStage,
        int remainderHundredths)
    {
        if (accumulatedSeconds < 0 ||
            accumulatedSeconds > MaxAccumulationSeconds)
        {
            throw new ArgumentOutOfRangeException(
                nameof(accumulatedSeconds));
        }

        if (remainderHundredths < 0 ||
            remainderHundredths >= PercentScale)
        {
            throw new ArgumentOutOfRangeException(
                nameof(remainderHundredths));
        }

        var productionPercent =
            CalculateProductionPercent(highestStage);
        var totalHundredths = checked(
            ((long)accumulatedSeconds *
             BaseGoldPerSecond *
             productionPercent) +
            remainderHundredths);

        return new IdleGoldCalculation(
            totalHundredths / PercentScale,
            (int)(totalHundredths % PercentScale),
            productionPercent);
    }
}
