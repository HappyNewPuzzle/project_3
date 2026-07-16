namespace IdleGuild.Api.Contracts;

public sealed record SyncProgressionRequest(
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
    int SkillThreeLevel);
