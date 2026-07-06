namespace IdleGuild.Domain.Heroes;

/// <summary>영웅 강화 시도의 성공 또는 거절 이유를 표현합니다.</summary>
public enum HeroUpgradeOutcome
{
    Succeeded = 1,
    InsufficientGold = 2,
    MaxLevelReached = 3
}
