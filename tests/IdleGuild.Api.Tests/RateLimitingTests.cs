using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IdleGuild.Api.Contracts;
using IdleGuild.Api.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace IdleGuild.Api.Tests;

/// <summary>익명 IP와 인증 플레이어별 요청 제한 및 429 오류 계약을 검증합니다.</summary>
public sealed class RateLimitingTests
{
    // 같은 IP의 게스트 생성이 허용량을 넘으면 DB 상태를 더 만들기 전에 거부해야 합니다.
    [Fact]
    public async Task GuestAccount_ExceedsIpLimit_ReturnsProblemDetails()
    {
        using var factory = new IdleGuildApiFactory();
        using var client = factory.CreateClient();

        for (var requestNumber = 0;
             requestNumber <
             ApiRateLimitPolicies.GuestAccountPermitLimit;
             requestNumber++)
        {
            using var allowed = await client.PostAsync(
                "/api/v1/accounts/guest",
                content: null);
            Assert.Equal(
                HttpStatusCode.Created,
                allowed.StatusCode);
        }

        using var rejected = await client.PostAsync(
            "/api/v1/accounts/guest",
            content: null);
        var problem = await rejected.Content
            .ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            rejected.StatusCode);
        Assert.Equal(
            "application/problem+json",
            rejected.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(rejected.Headers.RetryAfter);
        Assert.Equal("Too many requests.", problem?.Title);
        Assert.Equal(429, problem?.Status);
        Assert.True(problem?.Extensions.ContainsKey(
            "retryAfterSeconds"));
    }

    // 한 플레이어가 변경 한도를 소진해도 같은 IP의 다른 플레이어 요청은 허용되어야 합니다.
    [Fact]
    public async Task PlayerMutation_UsesAuthenticatedPlayerPartition()
    {
        using var factory = new IdleGuildApiFactory();
        using var client = factory.CreateClient();
        var firstGuest = await CreateGuestAsync(client);
        var secondGuest = await CreateGuestAsync(client);
        var idempotencyKey = $"rate-{Guid.NewGuid():N}";

        for (var requestNumber = 0;
             requestNumber <
             ApiRateLimitPolicies.PlayerMutationPermitLimit;
             requestNumber++)
        {
            using var request = CreateClaimRequest(
                firstGuest.AccessToken,
                idempotencyKey);
            using var allowed = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        }

        using var limitedRequest = CreateClaimRequest(
            firstGuest.AccessToken,
            idempotencyKey);
        using var limited = await client.SendAsync(
            limitedRequest);
        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            limited.StatusCode);

        using var otherPlayerRequest = CreateClaimRequest(
            secondGuest.AccessToken,
            $"rate-{Guid.NewGuid():N}");
        using var otherPlayerResponse = await client.SendAsync(
            otherPlayerRequest);
        Assert.Equal(
            HttpStatusCode.OK,
            otherPlayerResponse.StatusCode);
    }

    private static async Task<GuestAccountResponse>
        CreateGuestAsync(HttpClient client)
    {
        using var response = await client.PostAsync(
            "/api/v1/accounts/guest",
            content: null);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var guest = await response.Content
            .ReadFromJsonAsync<GuestAccountResponse>();
        return Assert.IsType<GuestAccountResponse>(guest);
    }

    private static HttpRequestMessage CreateClaimRequest(
        string accessToken,
        string idempotencyKey)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/rewards/idle/claim");
        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);
        request.Headers.Add(
            "Idempotency-Key",
            idempotencyKey);
        return request;
    }
}
