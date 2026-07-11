namespace IdleGuild.Domain.Shop;

/// <summary>서버가 판매를 허용한 모의 상품의 불변 정보를 표현합니다.</summary>
public sealed record ShopProduct(
    string ProductId,
    string Name,
    int MockPrice,
    long GoldAwarded);
