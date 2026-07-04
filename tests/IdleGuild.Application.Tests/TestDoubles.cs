using IdleGuild.Application.Abstractions.Authentication;
using IdleGuild.Application.Abstractions.Persistence;
using IdleGuild.Domain.GameStates;

namespace IdleGuild.Application.Tests;

/// <summary>Application 테스트가 DB 없이 저장 순서와 결과를 확인하게 합니다.</summary>
internal sealed class InMemoryPlayerGameStateRepository :
    IPlayerGameStateRepository,
    IGameUnitOfWork
{
    private readonly Dictionary<Guid, PlayerGameState> _states = [];

    public int SaveCount { get; private set; }

    public void Add(PlayerGameState gameState) =>
        _states.Add(gameState.PlayerId, gameState);

    public Task<PlayerGameState?> FindByIdAsync(
        Guid playerId,
        CancellationToken cancellationToken = default)
    {
        _states.TryGetValue(playerId, out var state);
        return Task.FromResult(state);
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.FromResult(1);
    }
}

/// <summary>Application 테스트에 예측 가능한 토큰 값을 제공합니다.</summary>
internal sealed class StubAccessTokenIssuer(
    string tokenValue,
    DateTimeOffset expiresAtUtc) : IAccessTokenIssuer
{
    public Guid IssuedPlayerId { get; private set; }

    public AccessToken Issue(Guid playerId)
    {
        IssuedPlayerId = playerId;
        return new AccessToken(tokenValue, expiresAtUtc);
    }
}

/// <summary>시간에 의존하는 유스케이스를 고정된 시각으로 검증하게 합니다.</summary>
internal sealed class StubTimeProvider(
    DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() =>
        utcNow;
}
