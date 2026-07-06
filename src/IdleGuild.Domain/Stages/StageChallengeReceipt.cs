using IdleGuild.Domain.Requests;

namespace IdleGuild.Domain.Stages;

/// <summary>스테이지 도전의 최초 성공 또는 실패 판정을 재현하는 영수증입니다.</summary>
public sealed class StageChallengeReceipt
{
    // EF Core가 PostgreSQL 행을 복원할 때 사용하는 전용 생성자입니다.
    private StageChallengeReceipt()
    {
        IdempotencyKey = string.Empty;
    }

    private StageChallengeReceipt(
        Guid playerId,
        string idempotencyKey,
        StageChallengeSettlement settlement)
    {
        PlayerId = playerId;
        IdempotencyKey = idempotencyKey;
        TargetStage = settlement.TargetStage;
        Outcome = settlement.Outcome;
        PreviousHighestStage =
            settlement.PreviousHighestStage;
        HighestStageAfter =
            settlement.HighestStageAfter;
        HeroPower = settlement.HeroPower;
        RequiredPower = settlement.RequiredPower;
        ProductionBonusPercentAfter =
            settlement.ProductionBonusPercentAfter;
        CheckpointGoldAwarded =
            settlement.CheckpointGoldAwarded;
        GoldBalanceAfter = settlement.GoldBalanceAfter;
        ProcessedAtUtc = settlement.ProcessedAtUtc;
    }

    public Guid PlayerId { get; private set; }

    public string IdempotencyKey { get; private set; }

    public int TargetStage { get; private set; }

    public StageChallengeOutcome Outcome { get; private set; }

    public int PreviousHighestStage { get; private set; }

    public int HighestStageAfter { get; private set; }

    public int HeroPower { get; private set; }

    public int RequiredPower { get; private set; }

    public int ProductionBonusPercentAfter { get; private set; }

    public long CheckpointGoldAwarded { get; private set; }

    public long GoldBalanceAfter { get; private set; }

    public DateTimeOffset ProcessedAtUtc { get; private set; }

    /// <summary>플레이어·멱등 키·판정 결과를 검증해 영수증을 생성합니다.</summary>
    public static StageChallengeReceipt Create(
        Guid playerId,
        string idempotencyKey,
        StageChallengeSettlement settlement)
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

        return new StageChallengeReceipt(
            playerId,
            idempotencyKey,
            settlement);
    }

    private static void ValidateSettlement(
        StageChallengeSettlement settlement)
    {
        StageChallengePolicy.ValidateStage(
            settlement.TargetStage);
        StageChallengePolicy.ValidateStage(
            settlement.PreviousHighestStage);
        StageChallengePolicy.ValidateStage(
            settlement.HighestStageAfter);

        var commonValuesAreValid =
            settlement.HeroPower >=
            StageChallengePolicy.BaseHeroPowerPerLevel &&
            settlement.RequiredPower >=
            StageChallengePolicy.BaseRequiredPower &&
            settlement.ProductionBonusPercentAfter >= 0 &&
            settlement.CheckpointGoldAwarded >= 0 &&
            settlement.GoldBalanceAfter >= 0 &&
            settlement.ProcessedAtUtc != default;
        var outcomeIsConsistent = settlement.Outcome switch
        {
            StageChallengeOutcome.Succeeded =>
                settlement.TargetStage ==
                settlement.PreviousHighestStage + 1 &&
                settlement.HighestStageAfter ==
                settlement.TargetStage &&
                settlement.HeroPower >=
                settlement.RequiredPower,
            StageChallengeOutcome.InsufficientPower =>
                settlement.TargetStage ==
                settlement.PreviousHighestStage + 1 &&
                settlement.HighestStageAfter ==
                settlement.PreviousHighestStage &&
                settlement.HeroPower <
                settlement.RequiredPower &&
                settlement.CheckpointGoldAwarded == 0,
            StageChallengeOutcome.AlreadyCompleted =>
                settlement.TargetStage <=
                settlement.PreviousHighestStage &&
                settlement.HighestStageAfter ==
                settlement.PreviousHighestStage &&
                settlement.CheckpointGoldAwarded == 0,
            StageChallengeOutcome.StageLocked =>
                settlement.TargetStage >
                settlement.PreviousHighestStage + 1 &&
                settlement.HighestStageAfter ==
                settlement.PreviousHighestStage &&
                settlement.CheckpointGoldAwarded == 0,
            _ => false
        };
        var bonusIsConsistent =
            settlement.ProductionBonusPercentAfter ==
            StageChallengePolicy
                .CalculateProductionBonusPercent(
                    settlement.HighestStageAfter);

        if (!commonValuesAreValid ||
            !outcomeIsConsistent ||
            !bonusIsConsistent)
        {
            throw new ArgumentException(
                "Stage challenge settlement contains invalid values.",
                nameof(settlement));
        }
    }
}
