namespace IdleGuild.Api.Contracts;

/// <summary>플레이어의 모의 구매 영수증 목록을 반환합니다.</summary>
public sealed record ShopPurchaseHistoryResponse(IReadOnlyList<ShopPurchaseResponse> Purchases);
