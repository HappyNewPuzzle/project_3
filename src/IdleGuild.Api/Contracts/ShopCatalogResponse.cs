namespace IdleGuild.Api.Contracts;

/// <summary>현재 판매 가능한 서버 상품 목록을 감쌉니다.</summary>
public sealed record ShopCatalogResponse(IReadOnlyList<ShopProductResponse> Products);
