using IdleGuild.Domain.Shop;

namespace IdleGuild.Application.Abstractions.Persistence;

/// <summary>구매 멱등 조회, 이력 조회와 신규 영수증 추가를 추상화합니다.</summary>
public interface IShopPurchaseRepository
{
    Task<ShopPurchaseReceipt?> FindAsync(Guid playerId, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShopPurchaseReceipt>> ListAsync(Guid playerId, CancellationToken cancellationToken = default);
    void Add(ShopPurchaseReceipt receipt);
}
