namespace IdleGuild.Api.Contracts;

/// <summary>클라이언트에 공개하는 모의 상품 정보를 표현합니다.</summary>
public sealed record ShopProductResponse(string ProductId, string Name, int MockPrice, long GoldAwarded);
