using IdleGuild.Domain.Requests;

namespace IdleGuild.Domain.Shop;

/// <summary>모의 구매의 최초 승인 결과와 지급 골드를 영구 보존합니다.</summary>
public sealed class ShopPurchaseReceipt
{
    private ShopPurchaseReceipt() { ProductId = string.Empty; IdempotencyKey = string.Empty; }

    private ShopPurchaseReceipt(Guid purchaseId, Guid playerId, string idempotencyKey,
        string productId, int mockPrice, long goldAwarded, long goldBalanceAfter,
        DateTimeOffset purchasedAtUtc)
    {
        PurchaseId = purchaseId;
        PlayerId = playerId;
        IdempotencyKey = idempotencyKey;
        ProductId = productId;
        MockPrice = mockPrice;
        GoldAwarded = goldAwarded;
        GoldBalanceAfter = goldBalanceAfter;
        PurchasedAtUtc = purchasedAtUtc;
    }

    public Guid PurchaseId { get; private set; }
    public Guid PlayerId { get; private set; }
    public string IdempotencyKey { get; private set; }
    public string ProductId { get; private set; }
    public int MockPrice { get; private set; }
    public long GoldAwarded { get; private set; }
    public long GoldBalanceAfter { get; private set; }
    public DateTimeOffset PurchasedAtUtc { get; private set; }

    public static ShopPurchaseReceipt Create(Guid playerId, string idempotencyKey,
        ShopProduct product, long goldBalanceAfter, DateTimeOffset purchasedAtUtc)
    {
        if (playerId == Guid.Empty) throw new ArgumentException("Player ID must not be empty.", nameof(playerId));
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentNullException.ThrowIfNull(product);
        var key = idempotencyKey.Trim();
        if (key.Length > IdempotencyPolicy.MaxKeyLength) throw new ArgumentOutOfRangeException(nameof(idempotencyKey));
        if (goldBalanceAfter < product.GoldAwarded) throw new ArgumentOutOfRangeException(nameof(goldBalanceAfter));
        if (purchasedAtUtc == default) throw new ArgumentException("Purchase time must be provided.", nameof(purchasedAtUtc));

        return new ShopPurchaseReceipt(Guid.NewGuid(), playerId, key, product.ProductId,
            product.MockPrice, product.GoldAwarded, goldBalanceAfter, purchasedAtUtc.ToUniversalTime());
    }
}
