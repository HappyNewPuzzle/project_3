using IdleGuild.Domain.Requests;

namespace IdleGuild.Domain.Heroes;

/// <summary>강화 요청의 최초 성공 또는 실패 결과를 재현하는 영수증입니다.</summary>
public sealed class HeroUpgradeReceipt
{
    // EF Core가 PostgreSQL 행을 복원할 때 사용하는 전용 생성자입니다.
    private HeroUpgradeReceipt()
    {
        IdempotencyKey = string.Empty;
    }

    private HeroUpgradeReceipt(
        Guid playerId,
        string idempotencyKey,
        HeroUpgradeSettlement settlement)
    {
        PlayerId = playerId;
        IdempotencyKey = idempotencyKey;
        Outcome = settlement.Outcome;
        PreviousLevel = settlement.PreviousLevel;
        HeroLevelAfter = settlement.HeroLevelAfter;
        GoldCost = settlement.GoldCost;
        GoldBalanceAfter = settlement.GoldBalanceAfter;
        ProcessedAtUtc = settlement.ProcessedAtUtc;
    }

    public Guid PlayerId { get; private set; }

    public string IdempotencyKey { get; private set; }

    public HeroUpgradeOutcome Outcome { get; private set; }

    public int PreviousLevel { get; private set; }

    public int HeroLevelAfter { get; private set; }

    public long GoldCost { get; private set; }

    public long GoldBalanceAfter { get; private set; }

    public DateTimeOffset ProcessedAtUtc { get; private set; }

    /// <summary>플레이어·멱등 키·판정 결과를 검증해 영수증을 생성합니다.</summary>
    public static HeroUpgradeReceipt Create(
        Guid playerId,
        string idempotencyKey,
        HeroUpgradeSettlement settlement)
    {
        if (playerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Player ID must not be empty.",
                nameof(playerId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(
            idempotencyKey);
        ArgumentNullException.ThrowIfNull(settlement);

        if (idempotencyKey.Length >
            IdempotencyPolicy.MaxKeyLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(idempotencyKey),
                $"Idempotency key cannot exceed {IdempotencyPolicy.MaxKeyLength} characters.");
        }

        ValidateSettlement(settlement);

        return new HeroUpgradeReceipt(
            playerId,
            idempotencyKey,
            settlement);
    }

    private static void ValidateSettlement(
        HeroUpgradeSettlement settlement)
    {
        var levelsAreValid =
            settlement.PreviousLevel >= 1 &&
            settlement.HeroLevelAfter >= 1 &&
            settlement.HeroLevelAfter <=
            HeroUpgradePolicy.MaxHeroLevel;
        var valuesAreValid =
            settlement.GoldCost >= 0 &&
            settlement.GoldBalanceAfter >= 0 &&
            settlement.ProcessedAtUtc != default;
        var outcomeIsConsistent = settlement.Outcome switch
        {
            HeroUpgradeOutcome.Succeeded =>
                settlement.HeroLevelAfter ==
                settlement.PreviousLevel + 1 &&
                settlement.GoldCost > 0,
            HeroUpgradeOutcome.InsufficientGold =>
                settlement.HeroLevelAfter ==
                settlement.PreviousLevel &&
                settlement.GoldCost >
                settlement.GoldBalanceAfter,
            HeroUpgradeOutcome.MaxLevelReached =>
                settlement.PreviousLevel ==
                HeroUpgradePolicy.MaxHeroLevel &&
                settlement.HeroLevelAfter ==
                HeroUpgradePolicy.MaxHeroLevel &&
                settlement.GoldCost == 0,
            _ => false
        };

        if (!levelsAreValid ||
            !valuesAreValid ||
            !outcomeIsConsistent)
        {
            throw new ArgumentException(
                "Hero upgrade settlement contains invalid values.",
                nameof(settlement));
        }
    }
}
