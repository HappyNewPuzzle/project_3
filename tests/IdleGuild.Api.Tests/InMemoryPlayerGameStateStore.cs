using System.Collections.Concurrent;
using IdleGuild.Application.Abstractions.Persistence;
using IdleGuild.Domain.GameStates;
using IdleGuild.Domain.Heroes;
using IdleGuild.Domain.Rewards;

namespace IdleGuild.Api.Tests;

/// <summary>API 인증 테스트가 PostgreSQL과 독립적으로 사용자 격리를 검증하게 합니다.</summary>
public sealed class InMemoryPlayerGameStateStore :
    IPlayerGameStateRepository,
    IIdleRewardClaimRepository,
    IHeroUpgradeReceiptRepository,
    IGameUnitOfWork
{
    private readonly ConcurrentDictionary<Guid, PlayerGameState> _states = [];
    private readonly ConcurrentDictionary<
        (Guid, string),
        IdleRewardClaimReceipt> _receipts = [];
    private readonly ConcurrentDictionary<
        (Guid, string),
        HeroUpgradeReceipt> _upgradeReceipts = [];

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

    public void Add(IdleRewardClaimReceipt receipt)
    {
        if (!_receipts.TryAdd(
                (receipt.PlayerId, receipt.IdempotencyKey),
                receipt))
        {
            throw new InvalidOperationException(
                "Idle reward receipt already exists.");
        }
    }

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
        HeroUpgradeReceipt receipt)
    {
        if (!_upgradeReceipts.TryAdd(
                (receipt.PlayerId, receipt.IdempotencyKey),
                receipt))
        {
            throw new InvalidOperationException(
                "Hero upgrade receipt already exists.");
        }
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(1);

    /// <summary>메모리 저장소에는 제거할 EF 변경 추적 상태가 없습니다.</summary>
    public void DiscardChanges()
    {
    }
}
