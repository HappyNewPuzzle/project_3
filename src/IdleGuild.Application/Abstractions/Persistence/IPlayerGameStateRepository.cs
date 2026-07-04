using IdleGuild.Domain.GameStates;

namespace IdleGuild.Application.Abstractions.Persistence;

/// <summary>Application이 저장 기술을 모르고 플레이어 상태를 다루게 합니다.</summary>
public interface IPlayerGameStateRepository
{
    void Add(PlayerGameState gameState);

    Task<PlayerGameState?> FindByIdAsync(
        Guid playerId,
        CancellationToken cancellationToken = default);
}
