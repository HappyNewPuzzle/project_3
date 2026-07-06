using IdleGuild.Domain.Stages;

namespace IdleGuild.Application.Stages.ChallengeStage;

/// <summary>스테이지 판정과 처리 후 진행 상태를 API에 전달합니다.</summary>
public sealed record ChallengeStageResult(
    string IdempotencyKey,
    int TargetStage,
    StageChallengeOutcome Outcome,
    int PreviousHighestStage,
    int HighestStageAfter,
    int HeroPower,
    int RequiredPower,
    int ProductionBonusPercentAfter,
    long CheckpointGoldAwarded,
    long GoldBalanceAfter,
    DateTimeOffset ProcessedAtUtc,
    bool IsReplay);
