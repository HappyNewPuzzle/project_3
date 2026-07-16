namespace IdleGuild.Domain.Rewards;

/// <summary>상태를 변경하지 않고 현재 시각에 받을 수 있는 방치 보상을 표현합니다.</summary>
public sealed record IdleRewardPreview(
    int ElapsedSeconds,
    long ClaimableGold,
    int MaximumAccumulationSeconds,
    DateTimeOffset CalculatedAtUtc);
