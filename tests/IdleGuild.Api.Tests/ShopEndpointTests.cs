using System.Net.Http.Headers;
using System.Net.Http.Json;
using IdleGuild.Api.Contracts;
using IdleGuild.Domain.Shop;

namespace IdleGuild.Api.Tests;

/// <summary>상품 조회, 구매 재생, 잔액과 구매 이력 HTTP 계약을 검증합니다.</summary>
public sealed class ShopEndpointTests(IdleGuildApiFactory factory) : IClassFixture<IdleGuildApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Purchase_ReplayAwardsOnceAndAppearsInHistory()
    {
        var guest = await (await _client.PostAsync("/api/v1/accounts/guest", null))
            .Content.ReadFromJsonAsync<GuestAccountResponse>();
        Assert.NotNull(guest);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", guest.AccessToken);

        var catalog = await _client.GetFromJsonAsync<ShopCatalogResponse>("/api/v1/shop/products");
        Assert.Equal(2, catalog!.Products.Count);
        var key = $"buy-{Guid.NewGuid():N}";
        var first = await PurchaseAsync(ShopCatalog.SmallGoldPackId, key);
        var replay = await PurchaseAsync(ShopCatalog.SmallGoldPackId, key);
        var state = await _client.GetFromJsonAsync<GameStateResponse>("/api/v1/game-state");
        var history = await _client.GetFromJsonAsync<ShopPurchaseHistoryResponse>("/api/v1/shop/purchases");

        Assert.False(first.IsReplay);
        Assert.True(replay.IsReplay);
        Assert.Equal(100, state!.Gold);
        Assert.Single(history!.Purchases);
    }

    private async Task<ShopPurchaseResponse> PurchaseAsync(string productId, string key)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"/api/v1/shop/products/{productId}/purchase");
        request.Headers.Add("Idempotency-Key", key);
        using var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ShopPurchaseResponse>())!;
    }
}
