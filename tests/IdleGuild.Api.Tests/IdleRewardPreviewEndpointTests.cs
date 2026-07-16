using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IdleGuild.Api.Contracts;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IdleGuild.Api.Tests;

/// <summary>미리보기 인증, 비변경 반복 조회와 같은 시각의 실제 수령을 검증합니다.</summary>
public sealed class IdleRewardPreviewEndpointTests(IdleGuildApiFactory factory) :
    IClassFixture<IdleGuildApiFactory>
{
    [Fact]
    public async Task Preview_WithoutToken_ReturnsUnauthorized()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/v1/rewards/idle/preview");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RepeatedPreview_DoesNotClaimAndClaimMatchesPreview()
    {
        var clock = new MutableTimeProvider(TimeProvider.System.GetUtcNow());
        using var configuredFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(clock);
            }));
        using var client = configuredFactory.CreateClient();
        var guest = await CreateGuestAsync(client);
        var store = configuredFactory.Services.GetRequiredService<InMemoryPlayerGameStateStore>();
        var before = await store.FindByIdAsync(guest.PlayerId);
        var beforeClaimedAt = before!.LastIdleRewardClaimedAtUtc;
        var beforeRemainder = before.IdleRewardRemainderHundredths;
        clock.UtcNow = clock.UtcNow.AddSeconds(3_600);

        var first = await PreviewAsync(client, guest.AccessToken);
        var second = await PreviewAsync(client, guest.AccessToken);
        var afterPreview = await store.FindByIdAsync(guest.PlayerId);

        Assert.Equal(3_600, first.ElapsedSeconds);
        Assert.Equal(3_600, first.ClaimableGold);
        Assert.Equal(first, second);
        Assert.Equal(0, afterPreview!.Gold);
        Assert.Equal(beforeClaimedAt, afterPreview.LastIdleRewardClaimedAtUtc);
        Assert.Equal(beforeRemainder, afterPreview.IdleRewardRemainderHundredths);

        using var claimRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/rewards/idle/claim");
        claimRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", guest.AccessToken);
        claimRequest.Headers.Add("Idempotency-Key", "preview-api-claim");
        using var claimResponse = await client.SendAsync(claimRequest);
        claimResponse.EnsureSuccessStatusCode();
        var claim = await claimResponse.Content.ReadFromJsonAsync<IdleRewardClaimResponse>();
        Assert.Equal(first.ClaimableGold, claim!.GoldAwarded);
    }

    private static async Task<GuestAccountResponse> CreateGuestAsync(HttpClient client)
    {
        using var response = await client.PostAsync("/api/v1/accounts/guest", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GuestAccountResponse>())!;
    }

    private static async Task<IdleRewardPreviewResponse> PreviewAsync(HttpClient client, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/rewards/idle/preview");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<IdleRewardPreviewResponse>())!;
    }

    private sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;
        public override DateTimeOffset GetUtcNow() => UtcNow;
    }
}
