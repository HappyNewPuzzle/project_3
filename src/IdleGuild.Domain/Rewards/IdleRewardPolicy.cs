namespace IdleGuild.Domain.Rewards;

/// <summary>MVP 방치 보상의 서버 권위형 고정 규칙을 정의합니다.</summary>
public static class IdleRewardPolicy
{
    public const int BaseGoldPerSecond = 1;

    public const int MaxAccumulationSeconds = 8 * 60 * 60;

}
