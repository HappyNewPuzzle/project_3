using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IdleGuild.Api.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace IdleGuild.Api.Tests;

/// <summary>스테이지 API의 인증, 진행 실패, 성공, 멱등 키 충돌을 검증합니다.</summary>
public sealed class StageChallengeEndpointTests(
    IdleGuildApiFactory factory) :
    IClassFixture<IdleGuildApiFactory>
{
    private readonly HttpClient _client =
        factory.CreateClient();
    private readonly InMemoryPlayerGameStateStore _store =
        factory.Services.GetRequiredService<
            InMemoryPlayerGameStateStore>();

    // 인증되지 않은 사용자는 스테이지 진행 상태를 바꿀 수 없어야 합니다.
    [Fact]
    public async Task Challenge_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsync(
            "/api/v1/stages/2/challenge",
            content: null);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    // 서버 콘텐츠 범위를 벗어난 스테이지는 판정 전에 거부해야 합니다.
    [Fact]
    public async Task Challenge_OutsideStageRange_ReturnsBadRequest()
    {
        var guest = await CreateGuestAsync();
        using var request = CreateChallengeRequest(
            guest.AccessToken,
            stage: 33,
            "invalid-stage");

        var response = await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    // 전투력 부족 결과는 같은 키의 네트워크 재시도에서도 그대로 재생해야 합니다.
    [Fact]
    public async Task Challenge_WithLowPower_ReplaysConflict()
    {
        var guest = await CreateGuestAsync();
        var key = $"stage-fail-{Guid.NewGuid():N}";

        var first = await SendChallengeAsync(
            guest.AccessToken,
            stage: 2,
            key);
        var replay = await SendChallengeAsync(
            guest.AccessToken,
            stage: 2,
            key);

        Assert.Equal(
            HttpStatusCode.Conflict,
            first.StatusCode);
        Assert.Equal(
            "insufficientPower",
            first.Body.Outcome);
        Assert.False(first.Body.IsReplay);
        Assert.True(replay.Body.IsReplay);
        Assert.Equal(1, replay.Body.HighestStageAfter);
    }

    // 충분한 전투력은 다음 스테이지와 5% 생산 보너스를 해금해야 합니다.
    [Fact]
    public async Task Challenge_WithEnoughPower_UpdatesGameState()
    {
        var guest = await CreateGuestAsync();
        var state = await _store.FindForUpdateAsync(
            guest.PlayerId);
        Assert.NotNull(state);
        state.ClaimIdleReward(
            state.LastIdleRewardClaimedAtUtc
                .AddSeconds(100));
        state.UpgradeMainHero(
            state.LastIdleRewardClaimedAtUtc);

        var challenge = await SendChallengeAsync(
            guest.AccessToken,
            stage: 2,
            $"stage-success-{Guid.NewGuid():N}");
        var gameState = await GetGameStateAsync(
            guest.AccessToken);

        Assert.Equal(
            HttpStatusCode.OK,
            challenge.StatusCode);
        Assert.Equal("succeeded", challenge.Body.Outcome);
        Assert.Equal(2, challenge.Body.HighestStageAfter);
        Assert.Equal(
            5,
            challenge.Body.ProductionBonusPercentAfter);
        Assert.Equal(2, gameState.HighestStage);
        Assert.Equal(5, gameState.ProductionBonusPercent);
    }

    // 같은 키를 다른 경로 파라미터에 사용하면 기존 결과를 잘못 재생하지 않아야 합니다.
    [Fact]
    public async Task Challenge_SameKeyDifferentStage_ReturnsConflict()
    {
        var guest = await CreateGuestAsync();
        var key = $"stage-reused-{Guid.NewGuid():N}";
        await SendChallengeAsync(
            guest.AccessToken,
            stage: 2,
            key);
        using var request = CreateChallengeRequest(
            guest.AccessToken,
            stage: 3,
            key);

        var response = await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
        var content = await response.Content
            .ReadAsStringAsync();
        Assert.Contains(
            "Idempotency key conflict",
            content,
            StringComparison.Ordinal);
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
        StageChallengeResponse Body)> SendChallengeAsync(
        string accessToken,
        int stage,
        string idempotencyKey)
    {
        using var request = CreateChallengeRequest(
            accessToken,
            stage,
            idempotencyKey);
        var response = await _client.SendAsync(request);
        var body = await response.Content
            .ReadFromJsonAsync<StageChallengeResponse>();

        return (
            response.StatusCode,
            Assert.IsType<StageChallengeResponse>(body));
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

    private static HttpRequestMessage
        CreateChallengeRequest(
            string accessToken,
            int stage,
            string idempotencyKey)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/stages/{stage}/challenge");
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
