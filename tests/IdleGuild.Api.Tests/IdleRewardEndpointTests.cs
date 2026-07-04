using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IdleGuild.Api.Contracts;

namespace IdleGuild.Api.Tests;

/// <summary>방치 보상 API의 인증·입력 검증·멱등 응답을 HTTP 수준에서 검증합니다.</summary>
public sealed class IdleRewardEndpointTests(
    IdleGuildApiFactory factory) :
    IClassFixture<IdleGuildApiFactory>
{
    private readonly HttpClient _client =
        factory.CreateClient();

    // 토큰이 없으면 보상 정산을 요청할 수 없어야 합니다.
    [Fact]
    public async Task Claim_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsync(
            "/api/v1/rewards/idle/claim",
            content: null);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    // 멱등 키가 없으면 안전한 중복 방지가 불가능하므로 요청을 거부해야 합니다.
    [Fact]
    public async Task Claim_WithoutIdempotencyKey_ReturnsBadRequest()
    {
        var guest = await CreateGuestAsync();
        using var request = CreateClaimRequest(
            guest.AccessToken,
            idempotencyKey: null);

        var response = await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    // 네트워크 재시도를 가정한 같은 키 요청은 최초 지급 결과를 그대로 재생해야 합니다.
    [Fact]
    public async Task Claim_WithSameKey_ReplaysFirstResponse()
    {
        var guest = await CreateGuestAsync();
        var key = $"claim-{Guid.NewGuid():N}";

        using var firstRequest = CreateClaimRequest(
            guest.AccessToken,
            key);
        var firstResponse = await _client.SendAsync(
            firstRequest);
        var first = await firstResponse.Content
            .ReadFromJsonAsync<IdleRewardClaimResponse>();

        using var replayRequest = CreateClaimRequest(
            guest.AccessToken,
            key);
        var replayResponse = await _client.SendAsync(
            replayRequest);
        var replay = await replayResponse.Content
            .ReadFromJsonAsync<IdleRewardClaimResponse>();

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, replayResponse.StatusCode);
        Assert.NotNull(first);
        Assert.NotNull(replay);
        Assert.False(first.IsReplay);
        Assert.True(replay.IsReplay);
        Assert.Equal(first.GoldAwarded, replay.GoldAwarded);
        Assert.Equal(first.GoldBalanceAfter, replay.GoldBalanceAfter);
        Assert.Equal(first.ClaimedAtUtc, replay.ClaimedAtUtc);
    }

    private async Task<GuestAccountResponse>
        CreateGuestAsync()
    {
        var response = await _client.PostAsync(
            "/api/v1/accounts/guest",
            content: null);
        var guest = await response.Content
            .ReadFromJsonAsync<GuestAccountResponse>();

        return Assert.IsType<GuestAccountResponse>(guest);
    }

    private static HttpRequestMessage CreateClaimRequest(
        string accessToken,
        string? idempotencyKey)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/rewards/idle/claim");
        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        if (idempotencyKey is not null)
        {
            request.Headers.Add(
                "Idempotency-Key",
                idempotencyKey);
        }

        return request;
    }
}
