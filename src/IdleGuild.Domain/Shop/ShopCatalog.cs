namespace IdleGuild.Domain.Shop;

/// <summary>클라이언트 입력과 분리된 서버 권위형 모의 상품 목록을 제공합니다.</summary>
public static class ShopCatalog
{
    public const string SmallGoldPackId = "small-gold-pack";
    public const string LargeGoldPackId = "large-gold-pack";
    public const int MaxProductIdLength = 64;

    private static readonly IReadOnlyDictionary<string, ShopProduct> Products =
        new Dictionary<string, ShopProduct>(StringComparer.Ordinal)
        {
            [SmallGoldPackId] = new(SmallGoldPackId, "Small Gold Pack", 100, 100),
            [LargeGoldPackId] = new(LargeGoldPackId, "Large Gold Pack", 450, 500)
        };

    public static IReadOnlyList<ShopProduct> List() =>
        Products.Values.OrderBy(product => product.MockPrice).ToArray();

    public static ShopProduct? Find(string productId) =>
        Products.GetValueOrDefault(productId);
}
