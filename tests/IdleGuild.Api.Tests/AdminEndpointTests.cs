using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IdleGuild.Api.Contracts;
using IdleGuild.Domain.Economy;
using IdleGuild.Domain.GameStates;
using IdleGuild.Infrastructure.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace IdleGuild.Api.Tests;

/// <summary>관리자 JWT 권한과 플레이어 상태·원장 조회 HTTP 계약을 검증합니다.</summary>
public sealed class AdminEndpointTests(
    IdleGuildApiFactory factory) :
    IClassFixture<IdleGuildApiFactory>
{
    private readonly HttpClient _client =
        factory.CreateClient();

    // 인증이 없으면 운영 데이터 Endpoint의 존재 여부와 내용을 조회할 수 없어야 합니다.
    [Fact]
    public async Task Player_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync(
            $"/api/v1/admin/players/{Guid.NewGuid():D}");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    // 정상 서명된 게스트 JWT도 admin Claim이 없으므로 운영 API에 접근할 수 없어야 합니다.
    [Fact]
    public async Task Player_WithGuestToken_ReturnsForbidden()
    {
        var guest = await CreateGuestAsync();
        using var request = CreateAuthorizedGet(
            $"/api/v1/admin/players/{guest.PlayerId:D}",
            guest.AccessToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    // 관리자 JWT는 지정한 플레이어의 서버 권위 상태를 읽을 수 있어야 합니다.
    [Fact]
    public async Task Player_WithAdminToken_ReturnsRequestedState()
    {
        var playerId = SeedPlayerWithLedger();
        using var request = CreateAuthorizedGet(
            $"/api/v1/admin/players/{playerId:D}",
            CreateAdminToken());

        var response = await _client.SendAsync(request);
        var player = await response.Content
            .ReadFromJsonAsync<AdminPlayerResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(player);
        Assert.Equal(playerId, player.PlayerId);
        Assert.Equal(20, player.Gold);
        Assert.Equal(2, player.HeroLevel);
        Assert.Equal(1, player.HighestStage);
    }

    // 반환된 다음 커서는 같은 플레이어의 더 오래된 원장을 중복 없이 이어서 조회해야 합니다.
    [Fact]
    public async Task GoldLedger_WithCursor_ReturnsNextPage()
    {
        var playerId = SeedPlayerWithLedger();
        var token = CreateAdminToken();
        using var firstRequest = CreateAuthorizedGet(
            $"/api/v1/admin/players/{playerId:D}/gold-ledger?pageSize=2",
            token);
        var firstResponse = await _client.SendAsync(
            firstRequest);
        var first = await firstResponse.Content
            .ReadFromJsonAsync<AdminGoldLedgerPageResponse>();

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.NotNull(first);
        Assert.Equal(2, first.Items.Count);
        Assert.Equal("heroUpgrade", first.Items[0].Reason);
        Assert.NotNull(first.NextCursor);

        using var nextRequest = CreateAuthorizedGet(
            $"/api/v1/admin/players/{playerId:D}/gold-ledger?pageSize=2&cursor={Uri.EscapeDataString(first.NextCursor)}",
            token);
        var nextResponse = await _client.SendAsync(
            nextRequest);
        var next = await nextResponse.Content
            .ReadFromJsonAsync<AdminGoldLedgerPageResponse>();

        Assert.Equal(HttpStatusCode.OK, nextResponse.StatusCode);
        Assert.NotNull(next);
        Assert.Single(next.Items);
        Assert.Equal("claim-1", next.Items[0].ReferenceId);
        Assert.Null(next.NextCursor);
    }

    // 임의 문자열 커서는 DB 조회 전에 400 ProblemDetails로 거부해야 합니다.
    [Fact]
    public async Task GoldLedger_WithInvalidCursor_ReturnsBadRequest()
    {
        using var request = CreateAuthorizedGet(
            $"/api/v1/admin/players/{Guid.NewGuid():D}/gold-ledger?cursor=invalid",
            CreateAdminToken());

        var response = await _client.SendAsync(request);
        var problem = await response.Content
            .ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
        Assert.Equal(
            "Ledger cursor is invalid.",
            problem?.Title);
    }

    private Guid SeedPlayerWithLedger()
    {
        var playerId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(
            2026, 7, 11, 0, 0, 0, TimeSpan.Zero);
        var state = PlayerGameState.Create(
            playerId,
            createdAt);
        var firstClaim = state.ClaimIdleReward(
            createdAt.AddSeconds(10));
        var secondClaim = state.ClaimIdleReward(
            createdAt.AddSeconds(30));
        var upgrade = state.UpgradeMainHero(
            createdAt.AddSeconds(31));
        var store = factory.Services.GetRequiredService<
            InMemoryPlayerGameStateStore>();
        store.Add(state);
        store.Add(GoldLedgerEntry.Create(
            playerId,
            GoldLedgerReason.IdleRewardClaim,
            balanceBefore: 0,
            firstClaim.GoldAwarded,
            firstClaim.GoldBalanceAfter,
            "claim-1",
            firstClaim.ClaimedAtUtc));
        store.Add(GoldLedgerEntry.Create(
            playerId,
            GoldLedgerReason.IdleRewardClaim,
            firstClaim.GoldBalanceAfter,
            secondClaim.GoldAwarded,
            secondClaim.GoldBalanceAfter,
            "claim-2",
            secondClaim.ClaimedAtUtc));
        store.Add(GoldLedgerEntry.Create(
            playerId,
            GoldLedgerReason.HeroUpgrade,
            secondClaim.GoldBalanceAfter,
            -upgrade.GoldCost,
            upgrade.GoldBalanceAfter,
            "upgrade-1",
            upgrade.ProcessedAtUtc));
        return playerId;
    }

    private string CreateAdminToken()
    {
        var options = factory.Services
            .GetRequiredService<JwtOptions>();
        return TestAdminTokenFactory.Create(options);
    }

    private async Task<GuestAccountResponse>
        CreateGuestAsync()
    {
        using var response = await _client.PostAsync(
            "/api/v1/accounts/guest",
            content: null);
        var guest = await response.Content
            .ReadFromJsonAsync<GuestAccountResponse>();
        return Assert.IsType<GuestAccountResponse>(guest);
    }

    private static HttpRequestMessage CreateAuthorizedGet(
        string path,
        string token)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            path);
        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);
        return request;
    }
}
