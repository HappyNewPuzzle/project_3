using IdleGuild.Domain.GameStates;
using IdleGuild.Domain.Shop;

namespace IdleGuild.Domain.Tests;

/// <summary>모의 상품 카탈로그와 서버 골드 지급 불변식을 검증합니다.</summary>
public sealed class ShopTests
{
    [Fact]
    public void Catalog_ContainsPositiveServerDefinedRewards()
    {
        var products = ShopCatalog.List();
        Assert.Equal(2, products.Count);
        Assert.All(products, product =>
        {
            Assert.True(product.MockPrice > 0);
            Assert.True(product.GoldAwarded > 0);
        });
    }

    [Fact]
    public void GrantShopGold_AddsOnlyPositiveAmount()
    {
        var state = PlayerGameState.Create(Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.Equal(100, state.GrantShopGold(100));
        Assert.Throws<ArgumentOutOfRangeException>(() => state.GrantShopGold(0));
    }
}
