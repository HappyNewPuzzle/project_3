using IdleGuild.Application.Abstractions.Persistence;

namespace IdleGuild.Application.Rewards.PreviewIdleReward;

/// <summary>읽기 전용 상태로 방치 보상을 계산하며 저장 작업을 수행하지 않습니다.</summary>
public sealed class PreviewIdleRewardHandler(
    IPlayerGameStateRepository repository,
    TimeProvider timeProvider)
{
    public async Task<IdleRewardPreviewResult?> HandleAsync(
        Guid playerId,
        CancellationToken cancellationToken = default)
    {
        if (playerId == Guid.Empty)
        {
            throw new ArgumentException("Player ID must not be empty.", nameof(playerId));
        }

        var gameState = await repository.FindByIdAsync(playerId, cancellationToken);
        if (gameState is null) return null;

        var preview = gameState.PreviewIdleReward(timeProvider.GetUtcNow());
        return new IdleRewardPreviewResult(
            preview.ElapsedSeconds,
            preview.ClaimableGold,
            preview.MaximumAccumulationSeconds,
            preview.CalculatedAtUtc);
    }
}
