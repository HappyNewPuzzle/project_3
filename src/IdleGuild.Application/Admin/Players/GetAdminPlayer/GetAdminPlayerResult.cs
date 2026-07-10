namespace IdleGuild.Application.Admin.Players.GetAdminPlayer;

/// <summary>운영자가 확인할 플레이어의 현재 서버 상태를 표현합니다.</summary>
public sealed record GetAdminPlayerResult(
    Guid PlayerId,
    long Gold,
    int HeroLevel,
    int HighestStage,
    int ProductionBonusPercent,
    int IdleRewardRemainderHundredths,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset LastIdleRewardClaimedAtUtc,
    uint Version);
