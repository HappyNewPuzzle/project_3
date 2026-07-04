using System.Collections.Concurrent;
using IdleGuild.Application.Abstractions.Persistence;
using IdleGuild.Domain.GameStates;

namespace IdleGuild.Api.Tests;

/// <summary>API 인증 테스트가 PostgreSQL과 독립적으로 사용자 격리를 검증하게 합니다.</summary>
public sealed class InMemoryPlayerGameStateStore :
    IPlayerGameStateRepository,
    IGameUnitOfWork
{
    private readonly ConcurrentDictionary<Guid, PlayerGameState> _states = [];

    public void Add(PlayerGameState gameState)
    {
        if (!_states.TryAdd(gameState.PlayerId, gameState))
        {
            throw new InvalidOperationException(
                "Player already exists.");
        }
    }

    public Task<PlayerGameState?> FindByIdAsync(
        Guid playerId,
        CancellationToken cancellationToken = default)
    {
        _states.TryGetValue(playerId, out var state);
        return Task.FromResult(state);
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(1);
}
