using System.Numerics;

namespace IdleGuild.Domain.Heroes;

/// <summary>주 영웅의 강화 비용과 안전한 최대 레벨 규칙을 정의합니다.</summary>
public static class HeroUpgradePolicy
{
    public const int BaseCost = 10;
    public const int GrowthNumerator = 115;
    public const int GrowthDenominator = 100;
    public const int MaxHeroLevel = 297;

    /// <summary>현재 레벨에서 다음 레벨로 오르는 비용을 정수 연산으로 계산합니다.</summary>
    public static long CalculateCost(int currentLevel)
    {
        if (currentLevel < 1 ||
            currentLevel >= MaxHeroLevel)
        {
            throw new ArgumentOutOfRangeException(
                nameof(currentLevel),
                $"Current level must be between 1 and {MaxHeroLevel - 1}.");
        }

        var exponent = currentLevel - 1;
        var scaledCost =
            (BigInteger)BaseCost *
            BigInteger.Pow(GrowthNumerator, exponent);
        var scale = BigInteger.Pow(
            GrowthDenominator,
            exponent);

        return checked((long)(scaledCost / scale));
    }
}
