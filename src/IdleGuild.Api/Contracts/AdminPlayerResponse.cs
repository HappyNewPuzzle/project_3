namespace IdleGuild.Api.Contracts;

/// <summary>관리자가 조회할 플레이어의 현재 서버 상태 계약입니다.</summary>
public sealed record AdminPlayerResponse(
    Guid PlayerId,
    long Gold,
    int HeroLevel,
    int HighestStage,
    int ProductionBonusPercent,
    int IdleRewardRemainderHundredths,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset LastIdleRewardClaimedAtUtc,
    uint Version);
