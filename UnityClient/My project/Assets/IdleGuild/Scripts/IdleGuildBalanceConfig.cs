using UnityEngine;

[CreateAssetMenu(fileName = "IdleGuildBalance", menuName = "Idle Guild/Balance Config")]
public sealed class IdleGuildBalanceConfig : ScriptableObject
{
    [Header("Hero")]
    public int baseAttack = 8;
    public int attackPerLevel = 4;
    public int attackPerEquipmentTier = 9;
    public float attackSpeedPerLevel = 0.08f;
    public float baseCriticalChance = 0.05f;
    public float criticalChancePerLevel = 0.015f;
    public float maximumCriticalChance = 0.5f;
    public float prestigeAttackBonus = 0.12f;

    [Header("Battle")]
    public int regularMonsterHealthInAttacks = 1;
    public int bossHealthInAttacks = 5;
    public int bossHealthPerStage = 8;
    public float bossTimeLimitSeconds = 10f;
    public float bossAttackIntervalSeconds = 2.2f;
    public int stagesPerRegion = 3;
    public int[] skillDamageMultipliers = { 4, 3, 2 };

    [Header("Rewards")]
    public int regularBaseGold = 3;
    public int regularGoldPerStage = 2;
    public int bossBaseGold = 12;
    public int bossGoldPerStage = 6;
    public float equipmentDropChance = 0.35f;
    public int maximumOfflineHours = 8;

    public static IdleGuildBalanceConfig LoadOrCreateRuntimeDefault()
    {
        IdleGuildBalanceConfig config = Resources.Load<IdleGuildBalanceConfig>("Balance/IdleGuildBalance");
        return config != null ? config : CreateInstance<IdleGuildBalanceConfig>();
    }
}
