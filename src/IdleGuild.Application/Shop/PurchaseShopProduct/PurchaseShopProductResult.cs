namespace IdleGuild.Application.Shop.PurchaseShopProduct;

/// <summary>모의 구매 승인과 골드 지급 결과를 전달합니다.</summary>
public sealed record PurchaseShopProductResult(Guid PurchaseId, string IdempotencyKey,
    string ProductId, int MockPrice, long GoldAwarded, long GoldBalanceAfter,
    DateTimeOffset PurchasedAtUtc, bool IsReplay);
