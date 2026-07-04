using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IdleGuild.Api.Contracts;

namespace IdleGuild.Api.Tests;

/// <summary>게스트 JWT 인증과 사용자별 게임 상태 격리를 HTTP 수준에서 검증합니다.</summary>
public sealed class AccountAuthenticationTests(
    IdleGuildApiFactory factory) :
    IClassFixture<IdleGuildApiFactory>
{
    private readonly HttpClient _client =
        factory.CreateClient();

    // 액세스 토큰이 없는 요청은 보호된 게임 상태를 읽을 수 없어야 합니다.
    [Fact]
    public async Task GameState_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync(
            "/api/v1/game-state");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    // 게스트 생성 응답의 토큰은 생성된 본인의 초기 상태만 조회해야 합니다.
    [Fact]
    public async Task GuestToken_ReadsCreatedPlayerState()
    {
        var guest = await CreateGuestAsync();

        var state = await GetGameStateAsync(
            guest.AccessToken);

        Assert.Equal(guest.PlayerId, state.PlayerId);
        Assert.Equal(0, state.Gold);
        Assert.Equal(1, state.HeroLevel);
        Assert.Equal(1, state.HighestStage);
    }

    // 서로 다른 토큰의 subject가 각기 다른 플레이어 상태로 연결되어야 합니다.
    [Fact]
    public async Task DifferentGuestTokens_ReadDifferentStates()
    {
        var firstGuest = await CreateGuestAsync();
        var secondGuest = await CreateGuestAsync();

        var firstState = await GetGameStateAsync(
            firstGuest.AccessToken);
        var secondState = await GetGameStateAsync(
            secondGuest.AccessToken);

        Assert.NotEqual(
            firstGuest.PlayerId,
            secondGuest.PlayerId);
        Assert.Equal(
            firstGuest.PlayerId,
            firstState.PlayerId);
        Assert.Equal(
            secondGuest.PlayerId,
            secondState.PlayerId);
    }

    // payload가 변조된 JWT는 서명 검증에 실패해 401을 반환해야 합니다.
    [Fact]
    public async Task GameState_WithTamperedToken_ReturnsUnauthorized()
    {
        var guest = await CreateGuestAsync();
        var tokenParts = guest.AccessToken.Split('.');
        tokenParts[1] =
            (tokenParts[1][0] == 'a' ? 'b' : 'a') +
            tokenParts[1][1..];
        var tamperedToken = string.Join('.', tokenParts);
        using var request = CreateGameStateRequest(
            tamperedToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    private async Task<GuestAccountResponse>
        CreateGuestAsync()
    {
        var response = await _client.PostAsync(
            "/api/v1/accounts/guest",
            content: null);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var guest = await response.Content
            .ReadFromJsonAsync<GuestAccountResponse>();

        return Assert.IsType<GuestAccountResponse>(guest);
    }

    private async Task<GameStateResponse>
        GetGameStateAsync(string accessToken)
    {
        using var request = CreateGameStateRequest(
            accessToken);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var state = await response.Content
            .ReadFromJsonAsync<GameStateResponse>();

        return Assert.IsType<GameStateResponse>(state);
    }

    private static HttpRequestMessage CreateGameStateRequest(
        string accessToken)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/v1/game-state");
        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        return request;
    }
}
