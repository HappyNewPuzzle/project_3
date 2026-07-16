namespace IdleGuild.Api.Contracts;

/// <summary>인증된 플레이어에게 노출할 현재 게임 상태입니다.</summary>
public sealed record GameStateResponse(
    Guid PlayerId,
    long Gold,
    int HeroLevel,
    int HeroPower,
    int EquipmentPowerBonus,
    int HighestStage,
    int AttackLevel,
    int AttackSpeedLevel,
    int CriticalLevel,
    int PrestigeLevel,
    int SoulStones,
    int EquipmentTier,
    int EquipmentCount,
    int UnlockedRegion,
    int SkillOneLevel,
    int SkillTwoLevel,
    int SkillThreeLevel,
    string SelectedHeroId,
    int ProductionBonusPercent,
    int IdleRewardRemainderHundredths,
    DateTimeOffset LastIdleRewardClaimedAtUtc);
