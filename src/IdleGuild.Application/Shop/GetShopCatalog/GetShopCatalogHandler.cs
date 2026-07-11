using IdleGuild.Domain.Shop;

namespace IdleGuild.Application.Shop.GetShopCatalog;

/// <summary>서버 카탈로그를 API용 결과로 반환합니다.</summary>
public sealed class GetShopCatalogHandler
{
    public IReadOnlyList<ShopProduct> Handle() => ShopCatalog.List();
}
