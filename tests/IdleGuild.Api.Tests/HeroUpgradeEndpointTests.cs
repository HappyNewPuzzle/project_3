using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IdleGuild.Api.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;

namespace IdleGuild.Api.Tests;

/// <summary>영웅 강화 API의 인증, 실패 계약, 성공 저장과 멱등 응답을 검증합니다.</summary>
public sealed class HeroUpgradeEndpointTests(
    IdleGuildApiFactory factory) :
    IClassFixture<IdleGuildApiFactory>
{
    private readonly HttpClient _client =
        factory.CreateClient();
    private readonly InMemoryPlayerGameStateStore _store =
        factory.Services.GetRequiredService<
            InMemoryPlayerGameStateStore>();

    // 토큰이 없는 사용자는 영웅 상태를 변경할 수 없어야 합니다.
    [Fact]
    public async Task Upgrade_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsync(
            "/api/v1/heroes/main/upgrade",
            content: null);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    // 멱등 키가 없으면 중복 차감을 막을 수 없으므로 요청을 거부해야 합니다.
    [Fact]
    public async Task Upgrade_WithoutIdempotencyKey_ReturnsBadRequest()
    {
        var guest = await CreateGuestAsync();
        using var request = CreateUpgradeRequest(
            guest.AccessToken,
            idempotencyKey: null);

        var response = await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
        var problem = await response.Content
            .ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(
            "Idempotency key is required.",
            problem?.Title);
    }

    // 골드 부족 결과도 같은 키 재요청에서 최초 판정 그대로 재생해야 합니다.
    [Fact]
    public async Task Upgrade_WithInsufficientGold_ReplaysConflict()
    {
        var guest = await CreateGuestAsync();
        var key = $"failed-{Guid.NewGuid():N}";

        var first = await SendUpgradeAsync(
            guest.AccessToken,
            key);
        var replay = await SendUpgradeAsync(
            guest.AccessToken,
            key);

        Assert.Equal(
            HttpStatusCode.Conflict,
            first.StatusCode);
        Assert.Equal(
            HttpStatusCode.Conflict,
            replay.StatusCode);
        Assert.Equal(
            "insufficientGold",
            first.Body.Outcome);
        Assert.False(first.Body.IsReplay);
        Assert.True(replay.Body.IsReplay);
        Assert.Equal(
            first.Body.ProcessedAtUtc,
            replay.Body.ProcessedAtUtc);
    }

    // 충분한 골드가 있으면 비용 차감과 레벨 증가가 조회 API에도 반영되어야 합니다.
    [Fact]
    public async Task Upgrade_WithEnoughGold_UpdatesGameState()
    {
        var guest = await CreateGuestAsync();
        var state = await _store.FindForUpdateAsync(
            guest.PlayerId);
        Assert.NotNull(state);
        state.ClaimIdleReward(
            state.LastIdleRewardClaimedAtUtc
                .AddSeconds(100));

        var upgrade = await SendUpgradeAsync(
            guest.AccessToken,
            $"success-{Guid.NewGuid():N}");
        var gameState = await GetGameStateAsync(
            guest.AccessToken);

        Assert.Equal(
            HttpStatusCode.OK,
            upgrade.StatusCode);
        Assert.Equal("succeeded", upgrade.Body.Outcome);
        Assert.Equal(10, upgrade.Body.GoldCost);
        Assert.Equal(2, upgrade.Body.HeroLevelAfter);
        Assert.Equal(90, upgrade.Body.GoldBalanceAfter);
        Assert.Equal(2, gameState.HeroLevel);
        Assert.Equal(90, gameState.Gold);
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

    private async Task<(
        HttpStatusCode StatusCode,
        HeroUpgradeResponse Body)> SendUpgradeAsync(
        string accessToken,
        string idempotencyKey)
    {
        using var request = CreateUpgradeRequest(
            accessToken,
            idempotencyKey);
        var response = await _client.SendAsync(request);
        var body = await response.Content
            .ReadFromJsonAsync<HeroUpgradeResponse>();

        return (
            response.StatusCode,
            Assert.IsType<HeroUpgradeResponse>(body));
    }

    private async Task<GameStateResponse>
        GetGameStateAsync(string accessToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/v1/game-state");
        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);
        var response = await _client.SendAsync(request);
        var body = await response.Content
            .ReadFromJsonAsync<GameStateResponse>();

        return Assert.IsType<GameStateResponse>(body);
    }

    private static HttpRequestMessage CreateUpgradeRequest(
        string accessToken,
        string? idempotencyKey)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v1/heroes/main/upgrade");
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
