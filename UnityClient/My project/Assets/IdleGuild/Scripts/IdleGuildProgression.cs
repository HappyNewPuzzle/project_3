using System;
using UnityEngine;

public sealed class IdleGuildProgression
{
    private const string Prefix = "IdleGuild.Progression.";
    public IdleGuildBalanceConfig Balance { get; }
    public IdleGuildEquipmentInventory Inventory { get; }

    public event Action Changed;

    public int Gold { get; private set; }
    public int Gems { get; private set; }
    public int Stage { get; private set; }
    public int AttackLevel { get; private set; }
    public int SpeedLevel { get; private set; }
    public int CriticalLevel { get; private set; }
    public int EquipmentTier { get; private set; }
    public int DefeatedMonsters { get; private set; }
    public int PendingOfflineGold { get; private set; }
    public int OfflineSeconds { get; private set; }
    public int EquipmentCount { get; private set; }
    public string EquippedItemName { get; private set; }
    public int PrestigeLevel { get; private set; }
    public int SoulStones { get; private set; }
    public int EquipmentMaterials { get; private set; }
    public int UnlockedRegion { get; private set; }
    public int SkillOneLevel { get; private set; }
    public int SkillTwoLevel { get; private set; }
    public int SkillThreeLevel { get; private set; }
    public int DailyStartKills { get; private set; }
    public int DailyStartStage { get; private set; }
    public int DailyStartAttackLevel { get; private set; }
    public int DailyClaimMask { get; private set; }
    public int AchievementClaimMask { get; private set; }
    public int DailyKillProgress => Mathf.Clamp(DefeatedMonsters - DailyStartKills, 0, 20);
    public int DailyBossProgress => Mathf.Clamp(Stage - DailyStartStage, 0, 1);
    public int DailyUpgradeProgress => Mathf.Clamp(AttackLevel - DailyStartAttackLevel, 0, 3);
    public string EquipmentRarity => EquipmentTier >= 8 ? "Legendary" : EquipmentTier >= 5 ? "Epic" : EquipmentTier >= 3 ? "Rare" : "Common";
    public string EquipmentSlot => EquipmentTier % 3 == 0 ? "Accessory" : EquipmentTier % 2 == 0 ? "Armor" : "Weapon";
    public float SkillCooldownRemaining => Mathf.Max(0f, skillReadyAt[selectedSkill] - Time.time);
    public bool IsSkillReady => SkillCooldownRemaining <= 0f;

    private readonly float[] skillReadyAt = new float[3];
    private int selectedSkill;

    public int AttackDamage => Mathf.RoundToInt((Balance.baseAttack + AttackLevel * Balance.attackPerLevel + EquipmentTier * Balance.attackPerEquipmentTier) * (1f + PrestigeLevel * Balance.prestigeAttackBonus));
    public float AttacksPerSecond => 1f + SpeedLevel * Balance.attackSpeedPerLevel;
    public float CriticalChance => Mathf.Min(Balance.maximumCriticalChance, Balance.baseCriticalChance + CriticalLevel * Balance.criticalChancePerLevel);
    public int CombatPower => Mathf.RoundToInt(AttackDamage * AttacksPerSecond * (1f + CriticalChance));
    public int DamagePerSecond => Mathf.RoundToInt(AttackDamage * AttacksPerSecond * (1f + CriticalChance));
    public float GoldPerSecond => Mathf.Max(1f, Stage * 3f * AttacksPerSecond / 2.2f);
    public int AttackUpgradeCost => 20 + AttackLevel * 15;
    public int SpeedUpgradeCost => 35 + SpeedLevel * 22;
    public int CriticalUpgradeCost => 50 + CriticalLevel * 30;

    public IdleGuildProgression(IdleGuildBalanceConfig balance = null, bool calculateOfflineReward = true)
    {
        Balance = balance != null ? balance : IdleGuildBalanceConfig.LoadOrCreateRuntimeDefault();
        Inventory = new IdleGuildEquipmentInventory();
        Gold = PlayerPrefs.GetInt(Prefix + "Gold", 100);
        Gems = PlayerPrefs.GetInt(Prefix + "Gems", 25);
        Stage = Mathf.Max(1, PlayerPrefs.GetInt(Prefix + "Stage", 1));
        AttackLevel = PlayerPrefs.GetInt(Prefix + "Attack", 1);
        SpeedLevel = PlayerPrefs.GetInt(Prefix + "Speed", 0);
        CriticalLevel = PlayerPrefs.GetInt(Prefix + "Critical", 0);
        EquipmentTier = PlayerPrefs.GetInt(Prefix + "Equipment", 0);
        DefeatedMonsters = PlayerPrefs.GetInt(Prefix + "Kills", 0);
        EquipmentCount = PlayerPrefs.GetInt(Prefix + "EquipmentCount", EquipmentTier > 0 ? 1 : 0);
        EquippedItemName = PlayerPrefs.GetString(Prefix + "EquippedName", EquipmentTier > 0 ? "Mountain Gear +" + EquipmentTier : "None");
        PrestigeLevel = PlayerPrefs.GetInt(Prefix + "Prestige", 0);
        SoulStones = PlayerPrefs.GetInt(Prefix + "SoulStones", 0);
        EquipmentMaterials = PlayerPrefs.GetInt(Prefix + "EquipmentMaterials", 0);
        UnlockedRegion = Mathf.Max(0, PlayerPrefs.GetInt(Prefix + "UnlockedRegion", 0));
        SkillOneLevel = Mathf.Max(1, PlayerPrefs.GetInt(Prefix + "SkillOne", 1));
        SkillTwoLevel = Mathf.Max(1, PlayerPrefs.GetInt(Prefix + "SkillTwo", 1));
        SkillThreeLevel = Mathf.Max(1, PlayerPrefs.GetInt(Prefix + "SkillThree", 1));
        LoadDailyMissions();
        AchievementClaimMask = PlayerPrefs.GetInt(Prefix + "AchievementClaims", 0);
        if (calculateOfflineReward)
        {
            CalculateOfflineReward();
        }
        else
        {
            // 영웅 변경 Scene 재로드는 방치가 아니므로 보상은 만들지 않고 시간 기준만 현재로 갱신합니다.
            PendingOfflineGold = 0;
            OfflineSeconds = 0;
            MarkExitTime();
        }
    }

    public bool UpgradeAttack() => SpendAndUpgrade(AttackUpgradeCost, () => AttackLevel++);
    public bool UpgradeSpeed() => SpendAndUpgrade(SpeedUpgradeCost, () => SpeedLevel++);
    public bool UpgradeCritical() => SpendAndUpgrade(CriticalUpgradeCost, () => CriticalLevel++);

    public bool ClaimDailyAttendance()
    {
        string today = DateTime.UtcNow.ToString("yyyyMMdd");
        if (PlayerPrefs.GetString(Prefix + "AttendanceDate", string.Empty) == today) return false;
        PlayerPrefs.SetString(Prefix + "AttendanceDate", today);
        Gold += 100 + Stage * 15;
        Gems += 3;
        SaveAndNotify();
        return true;
    }

    public bool ClaimWelcomeMail()
    {
        if (PlayerPrefs.GetInt(Prefix + "WelcomeMail", 0) == 1) return false;
        PlayerPrefs.SetInt(Prefix + "WelcomeMail", 1);
        Gold += 500;
        Gems += 10;
        SaveAndNotify();
        return true;
    }

    public bool ClaimDailyMission(int mission)
    {
        int bit = 1 << Mathf.Clamp(mission, 0, 2);
        bool complete = mission == 0 ? DailyKillProgress >= 20 : mission == 1 ? DailyBossProgress >= 1 : DailyUpgradeProgress >= 3;
        if (!complete || (DailyClaimMask & bit) != 0) return false;
        DailyClaimMask |= bit;
        Gold += mission == 0 ? 150 : mission == 1 ? 250 : 180;
        Gems += mission == 1 ? 2 : 1;
        SaveAndNotify();
        return true;
    }

    public bool ClaimAchievement(int achievement)
    {
        int bit = 1 << Mathf.Clamp(achievement, 0, 2);
        bool complete = achievement == 0 ? DefeatedMonsters >= 50 : achievement == 1 ? Stage >= 10 : EquipmentCount >= 5;
        if (!complete || (AchievementClaimMask & bit) != 0) return false;
        AchievementClaimMask |= bit;
        Gold += 500;
        Gems += 5;
        SaveAndNotify();
        return true;
    }

    public bool AutoEquip()
    {
        if (Inventory.AutoEquipBest())
        {
            ApplyEquippedInventoryItem();
            SaveAndNotify();
            return true;
        }

        int nextTier = EquipmentTier + 1;
        int cost = 80 * nextTier;
        return SpendAndUpgrade(cost, () =>
        {
            EquipmentTier = nextTier;
            EquipmentCount++;
            EquippedItemName = GetEquipmentName(EquipmentTier);
        });
    }

    public bool TryDropAndAutoEquip(bool boss)
    {
        float chance = boss ? Balance.equipmentDropChance : Balance.equipmentDropChance * 0.18f;
        if (UnityEngine.Random.value > chance)
        {
            return false;
        }

        IdleGuildEquipmentItem dropped = Inventory.Drop(Stage, boss);
        EquipmentCount = Inventory.Count;
        if (Inventory.Equipped == null || dropped.Score > Inventory.Equipped.Score)
        {
            Inventory.AutoEquipBest();
            ApplyEquippedInventoryItem();
        }
        SaveAndNotify();
        return true;
    }

    public bool SellSpareEquipment()
    {
        int earned = Inventory.SellUnequipped(false);
        if (earned <= 0) return false;
        Gold += earned;
        EquipmentCount = Inventory.Count;
        SaveAndNotify();
        return true;
    }

    public bool DisassembleSpareEquipment()
    {
        int materials = Inventory.SellUnequipped(true);
        if (materials <= 0) return false;
        EquipmentMaterials += materials;
        EquipmentCount = Inventory.Count;
        SaveAndNotify();
        return true;
    }

    public bool FuseEquipment()
    {
        if (EquipmentCount < 3) return false;
        EquipmentCount -= 2;
        EquipmentTier++;
        EquippedItemName = GetEquipmentName(EquipmentTier);
        SaveAndNotify();
        return true;
    }

    public bool Prestige()
    {
        if (Stage < 20) return false;
        SoulStones += Mathf.Max(1, Stage / 10);
        PrestigeLevel++;
        Stage = 1;
        AttackLevel = 1;
        SpeedLevel = 0;
        CriticalLevel = 0;
        SaveAndNotify();
        return true;
    }

    public bool UseSkill(int skillIndex = 0)
    {
        selectedSkill = Mathf.Clamp(skillIndex, 0, 2);
        if (!IsSkillReady)
        {
            return false;
        }

        skillReadyAt[selectedSkill] = Time.time + Mathf.Max(3f, 10f - GetSkillLevel(selectedSkill) * 0.35f - SpeedLevel * 0.1f);
        return true;
    }

    public int GetSkillLevel(int index) => index == 0 ? SkillOneLevel : index == 1 ? SkillTwoLevel : SkillThreeLevel;

    public bool UpgradeSkill(int index)
    {
        int level = GetSkillLevel(index);
        return SpendAndUpgrade(60 + level * 45, () =>
        {
            if (index == 0) SkillOneLevel++;
            else if (index == 1) SkillTwoLevel++;
            else SkillThreeLevel++;
        });
    }

    public int ClaimOfflineReward(int multiplier)
    {
        int awarded = PendingOfflineGold * Mathf.Clamp(multiplier, 1, 2);
        Gold += awarded;
        PendingOfflineGold = 0;
        OfflineSeconds = 0;
        SaveAndNotify();
        return awarded;
    }

    public void ApplyServerState(GameStateResponse state)
    {
        if (state == null) return;
        Gold = Mathf.Max(Gold, state.gold > int.MaxValue ? int.MaxValue : (int)state.gold);
        Stage = Mathf.Max(Stage, state.highestStage + 1);
        AttackLevel = Mathf.Max(AttackLevel, Mathf.Max(state.heroLevel, state.attackLevel));
        SpeedLevel = Mathf.Max(SpeedLevel, state.attackSpeedLevel);
        CriticalLevel = Mathf.Max(CriticalLevel, state.criticalLevel);
        PrestigeLevel = Mathf.Max(PrestigeLevel, state.prestigeLevel);
        SoulStones = Mathf.Max(SoulStones, state.soulStones);
        EquipmentTier = Mathf.Max(EquipmentTier, state.equipmentTier);
        EquipmentCount = Mathf.Max(EquipmentCount, state.equipmentCount);
        UnlockedRegion = Mathf.Max(UnlockedRegion, state.unlockedRegion);
        SkillOneLevel = Mathf.Max(SkillOneLevel, state.skillOneLevel);
        SkillTwoLevel = Mathf.Max(SkillTwoLevel, state.skillTwoLevel);
        SkillThreeLevel = Mathf.Max(SkillThreeLevel, state.skillThreeLevel);
        EquipmentTier = Mathf.Max(EquipmentTier, Mathf.Max(0, state.equipmentPowerBonus / 5));
        SaveAndNotify();
    }

    public void MarkExitTime()
    {
        PlayerPrefs.SetString(Prefix + "LastExitUtc", DateTime.UtcNow.Ticks.ToString());
        PlayerPrefs.Save();
    }

    public void CompleteDungeon()
    {
        Gems += 3;
        Gold += 30 + Stage * 10;
        SaveAndNotify();
    }

    public int RollDamage(out bool critical)
    {
        critical = UnityEngine.Random.value < CriticalChance;
        return critical ? AttackDamage * 2 : AttackDamage;
    }

    public int RewardFor(bool boss)
    {
        int reward = boss
            ? Balance.bossBaseGold + Stage * Balance.bossGoldPerStage
            : Balance.regularBaseGold + Stage * Balance.regularGoldPerStage;
        Gold += reward;
        DefeatedMonsters++;
        if (boss)
        {
            Stage++;
            UnlockedRegion = Mathf.Max(UnlockedRegion, (Stage - 1) / Mathf.Max(1, Balance.stagesPerRegion));
            if (Stage % 5 == 0)
            {
                Gems++;
            }
        }

        SaveAndNotify();
        return reward;
    }

    private bool SpendAndUpgrade(int cost, Action upgrade)
    {
        if (Gold < cost)
        {
            return false;
        }

        Gold -= cost;
        upgrade();
        SaveAndNotify();
        return true;
    }

    private void CalculateOfflineReward()
    {
        long ticks;
        string saved = PlayerPrefs.GetString(Prefix + "LastExitUtc", string.Empty);
        if (long.TryParse(saved, out ticks))
        {
            TimeSpan elapsed = DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc);
            OfflineSeconds = Mathf.Clamp((int)elapsed.TotalSeconds, 0, Balance.maximumOfflineHours * 60 * 60);
            PendingOfflineGold = Mathf.FloorToInt(OfflineSeconds * GoldPerSecond);
        }

        MarkExitTime();
    }

    private void LoadDailyMissions()
    {
        string today = DateTime.UtcNow.ToString("yyyyMMdd");
        if (PlayerPrefs.GetString(Prefix + "DailyMissionDate", string.Empty) != today)
        {
            PlayerPrefs.SetString(Prefix + "DailyMissionDate", today);
            DailyStartKills = DefeatedMonsters;
            DailyStartStage = Stage;
            DailyStartAttackLevel = AttackLevel;
            DailyClaimMask = 0;
            return;
        }

        DailyStartKills = PlayerPrefs.GetInt(Prefix + "DailyStartKills", DefeatedMonsters);
        DailyStartStage = PlayerPrefs.GetInt(Prefix + "DailyStartStage", Stage);
        DailyStartAttackLevel = PlayerPrefs.GetInt(Prefix + "DailyStartAttack", AttackLevel);
        DailyClaimMask = PlayerPrefs.GetInt(Prefix + "DailyClaims", 0);
    }

    private static string GetEquipmentName(int tier)
    {
        string[] names = { "Bronze Dagger", "Forest Bow", "Ruby Sword", "Moon Staff", "Dragon Relic" };
        return names[Mathf.Min(names.Length - 1, Mathf.Max(0, tier - 1))] + " +" + tier;
    }

    private void SaveAndNotify()
    {
        PlayerPrefs.SetInt(Prefix + "Gold", Gold);
        PlayerPrefs.SetInt(Prefix + "Gems", Gems);
        PlayerPrefs.SetInt(Prefix + "Stage", Stage);
        PlayerPrefs.SetInt(Prefix + "Attack", AttackLevel);
        PlayerPrefs.SetInt(Prefix + "Speed", SpeedLevel);
        PlayerPrefs.SetInt(Prefix + "Critical", CriticalLevel);
        PlayerPrefs.SetInt(Prefix + "Equipment", EquipmentTier);
        PlayerPrefs.SetInt(Prefix + "Kills", DefeatedMonsters);
        PlayerPrefs.SetInt(Prefix + "EquipmentCount", EquipmentCount);
        PlayerPrefs.SetString(Prefix + "EquippedName", EquippedItemName ?? "None");
        PlayerPrefs.SetInt(Prefix + "Prestige", PrestigeLevel);
        PlayerPrefs.SetInt(Prefix + "SoulStones", SoulStones);
        PlayerPrefs.SetInt(Prefix + "EquipmentMaterials", EquipmentMaterials);
        PlayerPrefs.SetInt(Prefix + "UnlockedRegion", UnlockedRegion);
        PlayerPrefs.SetInt(Prefix + "SkillOne", SkillOneLevel);
        PlayerPrefs.SetInt(Prefix + "SkillTwo", SkillTwoLevel);
        PlayerPrefs.SetInt(Prefix + "SkillThree", SkillThreeLevel);
        PlayerPrefs.SetInt(Prefix + "DailyStartKills", DailyStartKills);
        PlayerPrefs.SetInt(Prefix + "DailyStartStage", DailyStartStage);
        PlayerPrefs.SetInt(Prefix + "DailyStartAttack", DailyStartAttackLevel);
        PlayerPrefs.SetInt(Prefix + "DailyClaims", DailyClaimMask);
        PlayerPrefs.SetInt(Prefix + "AchievementClaims", AchievementClaimMask);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    private void ApplyEquippedInventoryItem()
    {
        IdleGuildEquipmentItem equipped = Inventory.Equipped;
        if (equipped == null) return;
        EquipmentTier = Mathf.Max(EquipmentTier, equipped.tier);
        EquipmentCount = Inventory.Count;
        EquippedItemName = equipped.name;
    }
}
