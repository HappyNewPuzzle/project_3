using IdleGuild.Application.Abstractions.Authentication;
using IdleGuild.Application.Abstractions.Persistence;
using IdleGuild.Domain.GameStates;
using IdleGuild.Domain.Heroes;
using IdleGuild.Domain.Rewards;
using IdleGuild.Domain.Stages;

namespace IdleGuild.Application.Tests;

/// <summary>Application 테스트가 DB 없이 저장 순서와 결과를 확인하게 합니다.</summary>
internal sealed class InMemoryPlayerGameStateRepository :
    IPlayerGameStateRepository,
    IIdleRewardClaimRepository,
    IHeroUpgradeReceiptRepository,
    IStageChallengeReceiptRepository,
    IGameUnitOfWork
{
    private readonly Dictionary<Guid, PlayerGameState> _states = [];
    private readonly Dictionary<(Guid, string), IdleRewardClaimReceipt>
        _receipts = [];
    private readonly Dictionary<(Guid, string), HeroUpgradeReceipt>
        _upgradeReceipts = [];
    private readonly Dictionary<(Guid, string), StageChallengeReceipt>
        _stageReceipts = [];

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

    public Task<PlayerGameState?> FindForUpdateAsync(
        Guid playerId,
        CancellationToken cancellationToken = default) =>
        FindByIdAsync(playerId, cancellationToken);

    public Task<IdleRewardClaimReceipt?> FindAsync(
        Guid playerId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        _receipts.TryGetValue(
            (playerId, idempotencyKey),
            out var receipt);
        return Task.FromResult(receipt);
    }

    public void Add(IdleRewardClaimReceipt receipt) =>
        _receipts.Add(
            (receipt.PlayerId, receipt.IdempotencyKey),
            receipt);

    Task<HeroUpgradeReceipt?>
        IHeroUpgradeReceiptRepository.FindAsync(
            Guid playerId,
            string idempotencyKey,
            CancellationToken cancellationToken)
    {
        _upgradeReceipts.TryGetValue(
            (playerId, idempotencyKey),
            out var receipt);
        return Task.FromResult(receipt);
    }

    void IHeroUpgradeReceiptRepository.Add(
        HeroUpgradeReceipt receipt) =>
        _upgradeReceipts.Add(
            (receipt.PlayerId, receipt.IdempotencyKey),
            receipt);

    Task<StageChallengeReceipt?>
        IStageChallengeReceiptRepository.FindAsync(
            Guid playerId,
            string idempotencyKey,
            CancellationToken cancellationToken)
    {
        _stageReceipts.TryGetValue(
            (playerId, idempotencyKey),
            out var receipt);
        return Task.FromResult(receipt);
    }

    void IStageChallengeReceiptRepository.Add(
        StageChallengeReceipt receipt) =>
        _stageReceipts.Add(
            (receipt.PlayerId, receipt.IdempotencyKey),
            receipt);

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.FromResult(1);
    }

    /// <summary>메모리 저장소에는 제거할 EF 변경 추적 상태가 없습니다.</summary>
    public void DiscardChanges()
    {
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
