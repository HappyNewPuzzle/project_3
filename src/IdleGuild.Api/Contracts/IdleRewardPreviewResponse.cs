namespace IdleGuild.Api.Contracts;

/// <summary>상태를 변경하지 않은 서버 시각 기준 방치 보상 예상값입니다.</summary>
public sealed record IdleRewardPreviewResponse(
    int ElapsedSeconds,
    long ClaimableGold,
    int MaximumAccumulationSeconds,
    DateTimeOffset CalculatedAtUtc);
