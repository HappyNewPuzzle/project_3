namespace IdleGuild.Application.Rewards.PreviewIdleReward;

/// <summary>API에 전달할 서버 시각 기준 방치 보상 미리보기 결과입니다.</summary>
public sealed record IdleRewardPreviewResult(
    int ElapsedSeconds,
    long ClaimableGold,
    int MaximumAccumulationSeconds,
    DateTimeOffset CalculatedAtUtc);
