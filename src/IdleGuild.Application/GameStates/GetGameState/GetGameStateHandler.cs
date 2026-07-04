using IdleGuild.Application.Abstractions.Persistence;

namespace IdleGuild.Application.GameStates.GetGameState;

/// <summary>인증된 플레이어 ID에 해당하는 게임 상태만 조회합니다.</summary>
public sealed class GetGameStateHandler(
    IPlayerGameStateRepository repository)
{
    /// <summary>저장된 상태를 외부 노출용 Application 결과로 변환합니다.</summary>
    public async Task<GetGameStateResult?> HandleAsync(
        Guid playerId,
        CancellationToken cancellationToken = default)
    {
        var gameState = await repository.FindByIdAsync(
            playerId,
            cancellationToken);

        return gameState is null
            ? null
            : new GetGameStateResult(
                gameState.PlayerId,
                gameState.Gold,
                gameState.HeroLevel,
                gameState.HighestStage,
                gameState.LastIdleRewardClaimedAtUtc);
    }
}
