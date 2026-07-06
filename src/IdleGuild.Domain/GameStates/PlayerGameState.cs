using IdleGuild.Domain.Heroes;
using IdleGuild.Domain.Rewards;

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
    }

    public Guid PlayerId { get; private set; }

    public long Gold { get; private set; }

    public int HeroLevel { get; private set; }

    public int HighestStage { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset LastIdleRewardClaimedAtUtc { get; private set; }

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
        var goldAwarded = checked(
            (long)accumulatedSeconds *
            IdleRewardPolicy.BaseGoldPerSecond);

        Gold = checked(Gold + goldAwarded);
        LastIdleRewardClaimedAtUtc = claimedAtUtc;

        return new IdleRewardSettlement(
            goldAwarded,
            accumulatedSeconds,
            Gold,
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
}
