using System.Numerics;
using IdleGuild.Domain.Heroes;
using IdleGuild.Domain.Rewards;

namespace IdleGuild.Domain.Stages;

/// <summary>스테이지 전투력, 도전 범위, 생산 보너스 규칙을 정의합니다.</summary>
public static class StageChallengePolicy
{
    public const int BaseHeroPowerPerLevel = 10;
    public const int BaseRequiredPower = 10;
    public const int RequiredPowerGrowthNumerator = 120;
    public const int RequiredPowerGrowthDenominator = 100;
    public const int MaxStage = 32;

    /// <summary>영웅 레벨로 서버 권위 전투력을 계산합니다.</summary>
    public static int CalculateHeroPower(int heroLevel)
    {
        if (heroLevel < 1 ||
            heroLevel > HeroUpgradePolicy.MaxHeroLevel)
        {
            throw new ArgumentOutOfRangeException(
                nameof(heroLevel));
        }

        return checked(
            heroLevel * BaseHeroPowerPerLevel);
    }

    /// <summary>지정 스테이지의 요구 전투력을 정확한 정수 연산으로 계산합니다.</summary>
    public static int CalculateRequiredPower(int stage)
    {
        ValidateStage(stage);
        var exponent = stage - 1;
        var scaledPower =
            (BigInteger)BaseRequiredPower *
            BigInteger.Pow(
                RequiredPowerGrowthNumerator,
                exponent);
        var scale = BigInteger.Pow(
            RequiredPowerGrowthDenominator,
            exponent);

        return checked((int)(scaledPower / scale));
    }

    /// <summary>기본 스테이지 이후 해금한 단계당 5% 생산 보너스를 반환합니다.</summary>
    public static int CalculateProductionBonusPercent(
        int highestStage)
    {
        ValidateStage(highestStage);

        return checked(
            (highestStage - 1) *
            IdleRewardPolicy.StageBonusPercent);
    }

    /// <summary>도전 가능한 콘텐츠 범위인지 검증합니다.</summary>
    public static void ValidateStage(int stage)
    {
        if (stage < 1 ||
            stage > MaxStage)
        {
            throw new ArgumentOutOfRangeException(
                nameof(stage),
                $"Stage must be between 1 and {MaxStage}.");
        }
    }
}
