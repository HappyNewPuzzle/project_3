namespace IdleGuild.Api.Contracts;

/// <summary>모의 구매 승인과 골드 지급 결과를 반환합니다.</summary>
public sealed record ShopPurchaseResponse(Guid PurchaseId, string IdempotencyKey, string ProductId,
    int MockPrice, long GoldAwarded, long GoldBalanceAfter, DateTimeOffset PurchasedAtUtc, bool IsReplay);
