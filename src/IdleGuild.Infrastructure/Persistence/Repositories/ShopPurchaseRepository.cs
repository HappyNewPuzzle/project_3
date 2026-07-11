using IdleGuild.Application.Abstractions.Persistence;
using IdleGuild.Domain.Shop;
using Microsoft.EntityFrameworkCore;

namespace IdleGuild.Infrastructure.Persistence.Repositories;

/// <summary>모의 구매 영수증의 멱등 조회와 최신순 이력을 PostgreSQL로 제공합니다.</summary>
public sealed class ShopPurchaseRepository(GameDbContext dbContext) : IShopPurchaseRepository
{
    public Task<ShopPurchaseReceipt?> FindAsync(Guid playerId, string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        dbContext.ShopPurchaseReceipts.AsNoTracking().SingleOrDefaultAsync(
            receipt => receipt.PlayerId == playerId && receipt.IdempotencyKey == idempotencyKey,
            cancellationToken);

    public async Task<IReadOnlyList<ShopPurchaseReceipt>> ListAsync(Guid playerId,
        CancellationToken cancellationToken = default) =>
        await dbContext.ShopPurchaseReceipts.AsNoTracking().Where(receipt => receipt.PlayerId == playerId)
            .OrderByDescending(receipt => receipt.PurchasedAtUtc).ThenByDescending(receipt => receipt.PurchaseId)
            .ToArrayAsync(cancellationToken);

    public void Add(ShopPurchaseReceipt receipt) => dbContext.ShopPurchaseReceipts.Add(receipt);
}
