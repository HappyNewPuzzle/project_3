using IdleGuild.Application.Shop.PurchaseShopProduct;
using IdleGuild.Domain.Economy;
using IdleGuild.Domain.GameStates;
using IdleGuild.Domain.Shop;
using IdleGuild.Infrastructure.Persistence;
using IdleGuild.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace IdleGuild.Infrastructure.Tests;

/// <summary>모의 구매의 상태·영수증·골드 원장 원자 저장을 PostgreSQL에서 검증합니다.</summary>
[Collection(PostgreSqlTestCollection.Name)]
public sealed class ShopPurchasePersistenceTests(PostgreSqlDatabaseFixture database)
{
    [Fact]
    public async Task PurchaseAndReplay_PersistOneReceiptAndLedger()
    {
        var playerId = Guid.NewGuid();
        await using (var seed = database.CreateDbContext())
        {
            seed.PlayerGameStates.Add(PlayerGameState.Create(playerId, DateTimeOffset.UtcNow));
            await seed.SaveChangesAsync();
        }

        await using (var context = database.CreateDbContext())
        {
            var handler = new PurchaseShopProductHandler(new PlayerGameStateRepository(context),
                new ShopPurchaseRepository(context), new GoldLedgerRepository(context),
                new EfGameUnitOfWork(context), TimeProvider.System);
            var first = await handler.HandleAsync(playerId, ShopCatalog.SmallGoldPackId, "purchase-db-1");
            var replay = await handler.HandleAsync(playerId, ShopCatalog.SmallGoldPackId, "purchase-db-1");
            Assert.False(first!.IsReplay);
            Assert.True(replay!.IsReplay);
        }

        await using var verify = database.CreateDbContext();
        Assert.Equal(100, (await verify.PlayerGameStates.SingleAsync(state => state.PlayerId == playerId)).Gold);
        Assert.Single(await verify.ShopPurchaseReceipts.Where(receipt => receipt.PlayerId == playerId).ToArrayAsync());
        Assert.Single(await verify.GoldLedgerEntries.Where(entry =>
            entry.PlayerId == playerId && entry.Reason == GoldLedgerReason.ShopPurchase).ToArrayAsync());
    }
}
