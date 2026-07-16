using IdleGuild.Domain.Heroes;
using IdleGuild.Domain.Rewards;
using IdleGuild.Domain.Stages;

namespace IdleGuild.Domain.GameStates;

/// <summary>플레이어 한 명의 재화와 핵심 진행 상태를 표현합니다.</summary>
public sealed class PlayerGameState
{
    // EF Core가 데이터베이스 값을 채울 때 사용하는 전용 생성자입니다.
    private PlayerGameState()
    {
    }

    private PlayerGameState(Guid playerId, DateTimeOffset createdAtUtc)
    {
        PlayerId = playerId;
        Gold = 0;
        HeroLevel = 1;
        HighestStage = 1;
        CreatedAtUtc = createdAtUtc;
        LastIdleRewardClaimedAtUtc = createdAtUtc;
        IdleRewardRemainderHundredths = 0;
        AttackLevel = 1;
        AttackSpeedLevel = 0;
        CriticalLevel = 0;
        PrestigeLevel = 0;
        SoulStones = 0;
        EquipmentTier = 0;
        EquipmentCount = 0;
        UnlockedRegion = 0;
        SkillOneLevel = 1;
        SkillTwoLevel = 1;
        SkillThreeLevel = 1;
    }

    public Guid PlayerId { get; private set; }

    public long Gold { get; private set; }

    public int HeroLevel { get; private set; }

    public int HighestStage { get; private set; }

    public int AttackLevel { get; private set; }
    public int AttackSpeedLevel { get; private set; }
    public int CriticalLevel { get; private set; }
    public int PrestigeLevel { get; private set; }
    public int SoulStones { get; private set; }
    public int EquipmentTier { get; private set; }
    public int EquipmentCount { get; private set; }
    public int UnlockedRegion { get; private set; }
    public int SkillOneLevel { get; private set; }
    public int SkillTwoLevel { get; private set; }
    public int SkillThreeLevel { get; private set; }

    public void SynchronizeProgression(
        int attackLevel,
        int attackSpeedLevel,
        int criticalLevel,
        int prestigeLevel,
        int soulStones,
        int equipmentTier,
        int equipmentCount,
        int unlockedRegion,
        int skillOneLevel,
        int skillTwoLevel,
        int skillThreeLevel)
    {
        if (attackLevel < 1 || attackSpeedLevel < 0 || criticalLevel < 0 || prestigeLevel < 0 || soulStones < 0 ||
            equipmentTier < 0 || equipmentCount < 0 || unlockedRegion < 0 || skillOneLevel < 1 || skillTwoLevel < 1 || skillThreeLevel < 1)
            throw new ArgumentOutOfRangeException(nameof(attackLevel));

        AttackLevel = Math.Max(AttackLevel, attackLevel);
        AttackSpeedLevel = Math.Max(AttackSpeedLevel, attackSpeedLevel);
        CriticalLevel = Math.Max(CriticalLevel, criticalLevel);
        PrestigeLevel = Math.Max(PrestigeLevel, prestigeLevel);
        SoulStones = Math.Max(SoulStones, soulStones);
        EquipmentTier = Math.Max(EquipmentTier, equipmentTier);
        EquipmentCount = Math.Max(EquipmentCount, equipmentCount);
        UnlockedRegion = Math.Max(UnlockedRegion, unlockedRegion);
        SkillOneLevel = Math.Max(SkillOneLevel, skillOneLevel);
        SkillTwoLevel = Math.Max(SkillTwoLevel, skillTwoLevel);
        SkillThreeLevel = Math.Max(SkillThreeLevel, skillThreeLevel);
    }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset LastIdleRewardClaimedAtUtc { get; private set; }

    public int IdleRewardRemainderHundredths { get; private set; }

    // PostgreSQL의 xmin 시스템 열과 연결되어 동시 수정을 감지합니다.
    public uint Version { get; private set; }

    /// <summary>검증된 플레이어 식별자와 서버 시각으로 초기 게임 상태를 생성합니다.</summary>
    public static PlayerGameState Create(
        Guid playerId,
        DateTimeOffset createdAt)
    {
        if (playerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Player ID must not be empty.",
                nameof(playerId));
        }

        if (createdAt == default)
        {
            throw new ArgumentException(
                "Creation time must be provided.",
                nameof(createdAt));
        }

        return new PlayerGameState(
            playerId,
            createdAt.ToUniversalTime());
    }

    /// <summary>서버 시각 기준 최대 8시간의 방치 골드를 정산하고 상태를 갱신합니다.</summary>
    public IdleRewardSettlement ClaimIdleReward(
        DateTimeOffset requestedAt)
    {
        if (requestedAt == default)
        {
            throw new ArgumentException(
                "Claim time must be provided.",
                nameof(requestedAt));
        }

        var requestedAtUtc = requestedAt.ToUniversalTime();
        var claimedAtUtc = requestedAtUtc <
            LastIdleRewardClaimedAtUtc
            ? LastIdleRewardClaimedAtUtc
            : requestedAtUtc;
        var elapsed = claimedAtUtc -
            LastIdleRewardClaimedAtUtc;
        var elapsedWholeSeconds =
            elapsed.Ticks / TimeSpan.TicksPerSecond;
        var accumulatedSeconds = (int)Math.Min(
            elapsedWholeSeconds,
            IdleRewardPolicy.MaxAccumulationSeconds);
        var calculation = IdleRewardPolicy.CalculateGold(
            accumulatedSeconds,
            HighestStage,
            IdleRewardRemainderHundredths);

        Gold = checked(Gold + calculation.GoldAwarded);
        IdleRewardRemainderHundredths =
            calculation.RemainderHundredths;
        LastIdleRewardClaimedAtUtc = claimedAtUtc;

        return new IdleRewardSettlement(
            calculation.GoldAwarded,
            accumulatedSeconds,
            Gold,
            calculation.RemainderHundredths,
            calculation.ProductionPercent,
            claimedAtUtc);
    }

    /// <summary>서버가 계산한 비용을 검사해 주 영웅을 한 레벨 강화합니다.</summary>
    public HeroUpgradeSettlement UpgradeMainHero(
        DateTimeOffset requestedAt)
    {
        if (requestedAt == default)
        {
            throw new ArgumentException(
                "Upgrade time must be provided.",
                nameof(requestedAt));
        }

        var processedAtUtc = requestedAt.ToUniversalTime();
        var previousLevel = HeroLevel;

        if (HeroLevel >= HeroUpgradePolicy.MaxHeroLevel)
        {
            return new HeroUpgradeSettlement(
                HeroUpgradeOutcome.MaxLevelReached,
                previousLevel,
                HeroLevel,
                GoldCost: 0,
                Gold,
                processedAtUtc);
        }

        var goldCost = HeroUpgradePolicy.CalculateCost(
            HeroLevel);

        if (Gold < goldCost)
        {
            return new HeroUpgradeSettlement(
                HeroUpgradeOutcome.InsufficientGold,
                previousLevel,
                HeroLevel,
                goldCost,
                Gold,
                processedAtUtc);
        }

        Gold -= goldCost;
        HeroLevel++;

        return new HeroUpgradeSettlement(
            HeroUpgradeOutcome.Succeeded,
            previousLevel,
            HeroLevel,
            goldCost,
            Gold,
            processedAtUtc);
    }

    /// <summary>서버가 승인한 상점 상품의 골드를 잔액에 더합니다.</summary>
    public long GrantShopGold(long amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        Gold = checked(Gold + amount);
        return Gold;
    }

    /// <summary>서버 전투력으로 스테이지를 판정하고 성공 시 생산 구간을 전환합니다.</summary>
    public StageChallengeSettlement ChallengeStage(
        int targetStage,
        DateTimeOffset requestedAt) =>
        ChallengeStage(
            targetStage,
            equipmentPowerBonus: 0,
            requestedAt);

    /// <summary>서버가 조회한 장착 장비 보너스를 포함해 스테이지 결과를 판정합니다.</summary>
    public StageChallengeSettlement ChallengeStage(
        int targetStage,
        int equipmentPowerBonus,
        DateTimeOffset requestedAt)
    {
        StageChallengePolicy.ValidateStage(targetStage);

        if (requestedAt == default)
        {
            throw new ArgumentException(
                "Challenge time must be provided.",
                nameof(requestedAt));
        }

        var processedAtUtc = requestedAt.ToUniversalTime();
        var previousHighestStage = HighestStage;
        var heroPower =
            StageChallengePolicy.CalculateHeroPower(
                HeroLevel,
                equipmentPowerBonus);
        var requiredPower =
            StageChallengePolicy.CalculateRequiredPower(
                targetStage);
        var outcome =
            DetermineStageChallengeOutcome(
                targetStage,
                heroPower,
                requiredPower);
        long checkpointGoldAwarded = 0;

        if (outcome == StageChallengeOutcome.Succeeded)
        {
            // 새 배율이 과거 방치 시간에 소급되지 않도록 기존 배율 구간을 먼저 정산합니다.
            var checkpoint = ClaimIdleReward(
                processedAtUtc);
            checkpointGoldAwarded =
                checkpoint.GoldAwarded;
            HighestStage = targetStage;
        }

        return new StageChallengeSettlement(
            targetStage,
            outcome,
            previousHighestStage,
            HighestStage,
            heroPower,
            requiredPower,
            StageChallengePolicy
                .CalculateProductionBonusPercent(
                    HighestStage),
            checkpointGoldAwarded,
            Gold,
            processedAtUtc);
    }

    private StageChallengeOutcome
        DetermineStageChallengeOutcome(
            int targetStage,
            int heroPower,
            int requiredPower)
    {
        if (targetStage <= HighestStage)
        {
            return StageChallengeOutcome.AlreadyCompleted;
        }

        if (targetStage > HighestStage + 1)
        {
            return StageChallengeOutcome.StageLocked;
        }

        return heroPower >= requiredPower
            ? StageChallengeOutcome.Succeeded
            : StageChallengeOutcome.InsufficientPower;
    }
}
