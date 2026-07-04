namespace IdleGuild.Api.Contracts;

/// <summary>인증된 플레이어에게 노출할 현재 게임 상태입니다.</summary>
public sealed record GameStateResponse(
    Guid PlayerId,
    long Gold,
    int HeroLevel,
    int HighestStage,
    DateTimeOffset LastIdleRewardClaimedAtUtc);
