namespace IdleGuild.Application.GameStates.GetGameState;

/// <summary>인증된 플레이어에게 노출할 현재 게임 상태를 표현합니다.</summary>
public sealed record GetGameStateResult(
    Guid PlayerId,
    long Gold,
    int HeroLevel,
    int HighestStage,
    int ProductionBonusPercent,
    int IdleRewardRemainderHundredths,
    DateTimeOffset LastIdleRewardClaimedAtUtc);
