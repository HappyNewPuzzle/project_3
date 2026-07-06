namespace IdleGuild.Domain.Stages;

/// <summary>한 번의 결정론적 스테이지 판정과 처리 직후 상태를 전달합니다.</summary>
public sealed record StageChallengeSettlement(
    int TargetStage,
    StageChallengeOutcome Outcome,
    int PreviousHighestStage,
    int HighestStageAfter,
    int HeroPower,
    int RequiredPower,
    int ProductionBonusPercentAfter,
    long CheckpointGoldAwarded,
    long GoldBalanceAfter,
    DateTimeOffset ProcessedAtUtc);
