namespace IdleGuild.Api.Contracts;

/// <summary>스테이지 판정, 전투력, 진행도와 생산 보너스를 클라이언트에 전달합니다.</summary>
public sealed record StageChallengeResponse(
    string IdempotencyKey,
    int TargetStage,
    string Outcome,
    int PreviousHighestStage,
    int HighestStageAfter,
    int HeroPower,
    int RequiredPower,
    int ProductionBonusPercentAfter,
    long CheckpointGoldAwarded,
    long GoldBalanceAfter,
    DateTimeOffset ProcessedAtUtc,
    bool IsReplay);
