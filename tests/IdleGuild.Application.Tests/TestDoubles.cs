using IdleGuild.Application.Abstractions.Authentication;
using IdleGuild.Application.Abstractions.Persistence;
using IdleGuild.Domain.Economy;
using IdleGuild.Domain.Equipment;
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
    IGoldLedgerRepository,
    IGoldLedgerReader,
    IPlayerEquipmentRepository,
    IEquipmentChangeReceiptRepository,
    IGameUnitOfWork
{
    private readonly Dictionary<Guid, PlayerGameState> _states = [];
    private readonly Dictionary<(Guid, string), IdleRewardClaimReceipt>
        _receipts = [];
    private readonly Dictionary<(Guid, string), HeroUpgradeReceipt>
        _upgradeReceipts = [];
    private readonly Dictionary<(Guid, string), StageChallengeReceipt>
        _stageReceipts = [];
    private readonly List<GoldLedgerEntry> _goldLedgerEntries = [];
    private readonly Dictionary<Guid, PlayerEquipment> _equipment = [];
    private readonly Dictionary<
        (Guid, string),
        EquipmentChangeReceipt> _equipmentReceipts = [];

    public int SaveCount { get; private set; }

    public IReadOnlyList<GoldLedgerEntry> GoldLedgerEntries =>
        _goldLedgerEntries;

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

    /// <summary>유스케이스가 만든 골드 변경 원장을 테스트에서 확인할 수 있게 보관합니다.</summary>
    public void Add(GoldLedgerEntry entry) =>
        _goldLedgerEntries.Add(entry);

    public void Add(PlayerEquipment equipment) =>
        _equipment.Add(equipment.EquipmentId, equipment);

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
        EquipmentChangeReceipt receipt) =>
        _equipmentReceipts.Add(
            (receipt.PlayerId, receipt.IdempotencyKey),
            receipt);

    /// <summary>관리자 Handler 테스트에 최신순 키셋 골드 원장을 제공합니다.</summary>
    public Task<IReadOnlyList<GoldLedgerEntry>>
        ListByPlayerAsync(
            Guid playerId,
            int take,
            GoldLedgerPagePosition? before,
            CancellationToken cancellationToken = default)
    {
        var entries = _goldLedgerEntries
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
