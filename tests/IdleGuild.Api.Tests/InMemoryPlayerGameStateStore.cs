using System.Collections.Concurrent;
using IdleGuild.Application.Abstractions.Persistence;
using IdleGuild.Domain.Economy;
using IdleGuild.Domain.Equipment;
using IdleGuild.Domain.GameStates;
using IdleGuild.Domain.Heroes;
using IdleGuild.Domain.Rewards;
using IdleGuild.Domain.Stages;
using IdleGuild.Domain.Shop;

namespace IdleGuild.Api.Tests;

/// <summary>API 인증 테스트가 PostgreSQL과 독립적으로 사용자 격리를 검증하게 합니다.</summary>
public sealed class InMemoryPlayerGameStateStore :
    IPlayerGameStateRepository,
    IIdleRewardClaimRepository,
    IHeroUpgradeReceiptRepository,
    IStageChallengeReceiptRepository,
    IGoldLedgerRepository,
    IGoldLedgerReader,
    IPlayerEquipmentRepository,
    IEquipmentChangeReceiptRepository,
    IShopPurchaseRepository,
    IGameUnitOfWork
{
    private readonly ConcurrentDictionary<Guid, PlayerGameState> _states = [];
    private readonly ConcurrentDictionary<
        (Guid, string),
        IdleRewardClaimReceipt> _receipts = [];
    private readonly ConcurrentDictionary<
        (Guid, string),
        HeroUpgradeReceipt> _upgradeReceipts = [];
    private readonly ConcurrentDictionary<
        (Guid, string),
        StageChallengeReceipt> _stageReceipts = [];
    private readonly ConcurrentDictionary<
        (Guid, GoldLedgerReason, string),
        GoldLedgerEntry> _goldLedgerEntries = [];
    private readonly ConcurrentDictionary<Guid, PlayerEquipment>
        _equipment = [];
    private readonly ConcurrentDictionary<
        (Guid, string),
        EquipmentChangeReceipt> _equipmentReceipts = [];
    private readonly ConcurrentDictionary<(Guid, string), ShopPurchaseReceipt> _shopReceipts = [];

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
        StageChallengeReceipt receipt)
    {
        if (!_stageReceipts.TryAdd(
                (receipt.PlayerId, receipt.IdempotencyKey),
                receipt))
        {
            throw new InvalidOperationException(
                "Stage challenge receipt already exists.");
        }
    }

    /// <summary>API 테스트에서도 같은 기능·참조 키의 원장을 한 번만 보관합니다.</summary>
    public void Add(GoldLedgerEntry entry)
    {
        if (!_goldLedgerEntries.TryAdd(
                (entry.PlayerId,
                    entry.Reason,
                    entry.ReferenceId),
                entry))
        {
            throw new InvalidOperationException(
                "Gold ledger entry already exists.");
        }
    }

    public void Add(PlayerEquipment equipment)
    {
        if (!_equipment.TryAdd(
                equipment.EquipmentId,
                equipment))
        {
            throw new InvalidOperationException(
                "Equipment already exists.");
        }
    }

    public Task<IReadOnlyList<PlayerEquipment>> ListAsync(
        Guid playerId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PlayerEquipment>>(
            _equipment.Values.Where(item =>
                item.PlayerId == playerId).ToArray());

    public Task<IReadOnlyList<PlayerEquipment>>
        ListEquippedAsync(
            Guid playerId,
            CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PlayerEquipment>>(
            _equipment.Values.Where(item =>
                item.PlayerId == playerId &&
                item.IsEquipped).ToArray());

    public Task<PlayerEquipment?> FindForUpdateAsync(
        Guid playerId,
        Guid equipmentId,
        CancellationToken cancellationToken = default)
    {
        _equipment.TryGetValue(equipmentId, out var item);
        return Task.FromResult(
            item?.PlayerId == playerId ? item : null);
    }

    public Task<PlayerEquipment?> FindEquippedForUpdateAsync(
        Guid playerId,
        EquipmentSlot slot,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_equipment.Values.SingleOrDefault(
            item =>
                item.PlayerId == playerId &&
                item.Slot == slot &&
                item.IsEquipped));

    Task<EquipmentChangeReceipt?>
        IEquipmentChangeReceiptRepository.FindAsync(
            Guid playerId,
            string idempotencyKey,
            CancellationToken cancellationToken)
    {
        _equipmentReceipts.TryGetValue(
            (playerId, idempotencyKey),
            out var receipt);
        return Task.FromResult(receipt);
    }

    void IEquipmentChangeReceiptRepository.Add(
        EquipmentChangeReceipt receipt)
    {
        if (!_equipmentReceipts.TryAdd(
                (receipt.PlayerId, receipt.IdempotencyKey),
                receipt))
        {
            throw new InvalidOperationException(
                "Equipment receipt already exists.");
        }
    }

    Task<ShopPurchaseReceipt?> IShopPurchaseRepository.FindAsync(Guid playerId, string idempotencyKey,
        CancellationToken cancellationToken)
    {
        _shopReceipts.TryGetValue((playerId, idempotencyKey), out var receipt);
        return Task.FromResult(receipt);
    }

    Task<IReadOnlyList<ShopPurchaseReceipt>> IShopPurchaseRepository.ListAsync(Guid playerId,
        CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<ShopPurchaseReceipt>>(
        _shopReceipts.Values.Where(receipt => receipt.PlayerId == playerId)
            .OrderByDescending(receipt => receipt.PurchasedAtUtc).ToArray());

    void IShopPurchaseRepository.Add(ShopPurchaseReceipt receipt)
    {
        if (!_shopReceipts.TryAdd((receipt.PlayerId, receipt.IdempotencyKey), receipt))
            throw new InvalidOperationException("Shop purchase receipt already exists.");
    }

    /// <summary>관리자 API 테스트에 플레이어별 최신순 원장 페이지를 제공합니다.</summary>
    public Task<IReadOnlyList<GoldLedgerEntry>>
        ListByPlayerAsync(
            Guid playerId,
            int take,
            GoldLedgerPagePosition? before,
            CancellationToken cancellationToken = default)
    {
        var entries = _goldLedgerEntries.Values
            .Where(entry => entry.PlayerId == playerId)
            .Where(entry => before is null ||
                entry.OccurredAtUtc < before.OccurredAtUtc ||
                (entry.OccurredAtUtc == before.OccurredAtUtc &&
                 entry.EntryId.CompareTo(before.EntryId) < 0))
            .OrderByDescending(entry => entry.OccurredAtUtc)
            .ThenByDescending(entry => entry.EntryId)
            .Take(take)
            .ToArray();
        return Task.FromResult<
            IReadOnlyList<GoldLedgerEntry>>(entries);
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(1);

    /// <summary>메모리 저장소에는 제거할 EF 변경 추적 상태가 없습니다.</summary>
    public void DiscardChanges()
    {
    }
}
