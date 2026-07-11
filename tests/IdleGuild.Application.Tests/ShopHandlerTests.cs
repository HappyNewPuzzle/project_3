using IdleGuild.Application.Abstractions.Persistence;
using IdleGuild.Application.Shop.GetPurchaseHistory;
using IdleGuild.Application.Shop.PurchaseShopProduct;
using IdleGuild.Domain.Economy;
using IdleGuild.Domain.GameStates;
using IdleGuild.Domain.Shop;

namespace IdleGuild.Application.Tests;

/// <summary>구매 지급, 멱등 재생, 키 충돌과 구매 이력을 검증합니다.</summary>
public sealed class ShopHandlerTests
{
    [Fact]
    public async Task Purchase_ReplayAwardsGoldAndLedgerOnce()
    {
        var repository = new InMemoryPlayerGameStateRepository();
        var playerId = Guid.NewGuid();
        repository.Add(PlayerGameState.Create(playerId, DateTimeOffset.UtcNow));
        var handler = new PurchaseShopProductHandler(repository, repository, repository, repository,
            new StubTimeProvider(DateTimeOffset.UtcNow));

        var first = await handler.HandleAsync(playerId, ShopCatalog.SmallGoldPackId, "buy-1");
        var replay = await handler.HandleAsync(playerId, ShopCatalog.SmallGoldPackId, "buy-1");

        Assert.NotNull(first);
        Assert.False(first.IsReplay);
        Assert.True(replay!.IsReplay);
        Assert.Equal(100, (await repository.FindByIdAsync(playerId))!.Gold);
        var ledger = Assert.Single(repository.GoldLedgerEntries);
        Assert.Equal(GoldLedgerReason.ShopPurchase, ledger.Reason);
    }

    [Fact]
    public async Task Purchase_SameKeyDifferentProductThrowsConflictAndHistoryHasOne()
    {
        var repository = new InMemoryPlayerGameStateRepository();
        var playerId = Guid.NewGuid();
        repository.Add(PlayerGameState.Create(playerId, DateTimeOffset.UtcNow));
        var handler = new PurchaseShopProductHandler(repository, repository, repository, repository,
            new StubTimeProvider(DateTimeOffset.UtcNow));
        await handler.HandleAsync(playerId, ShopCatalog.SmallGoldPackId, "buy-1");

        await Assert.ThrowsAsync<IdempotencyKeyConflictException>(() =>
            handler.HandleAsync(playerId, ShopCatalog.LargeGoldPackId, "buy-1"));
        var history = await new GetPurchaseHistoryHandler(repository, repository).HandleAsync(playerId);
        Assert.Single(history!);
    }
}
