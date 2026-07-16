using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IdleGuild.Api.Contracts;
using IdleGuild.Domain.GameStates;
using Microsoft.AspNetCore.Mvc;

namespace IdleGuild.Api.Tests;

/// <summary>선택 영웅 기본값, 저장, 검증과 플레이어 격리를 HTTP 수준에서 검증합니다.</summary>
public sealed class ProfileEndpointTests(IdleGuildApiFactory factory) :
    IClassFixture<IdleGuildApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task UpdateSelectedHero_WithoutToken_ReturnsUnauthorized()
    {
        using var response = await _client.PutAsJsonAsync(
            "/api/v1/profile/selected-hero",
            new UpdateSelectedHeroRequest("black_cat"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task NewGuest_CanSelectBlackCatThenClassicAndReloadState()
    {
        var guest = await CreateGuestAsync();
        var initial = await GetStateAsync(_client, guest.AccessToken);
        Assert.Equal(SelectedHeroPolicy.DefaultHeroId, initial.SelectedHeroId);

        var unchanged = await UpdateAsync(guest.AccessToken, "girl");
        Assert.Equal("girl", unchanged.SelectedHeroId);

        var blackCat = await UpdateAsync(guest.AccessToken, "black_cat");
        Assert.Equal("black_cat", blackCat.SelectedHeroId);
        Assert.Equal("black_cat", (await GetStateAsync(_client, guest.AccessToken)).SelectedHeroId);

        var classic = await UpdateAsync(guest.AccessToken, "classic");
        Assert.Equal("classic", classic.SelectedHeroId);
        using var reloadedClient = factory.CreateClient();
        Assert.Equal("classic", (await GetStateAsync(reloadedClient, guest.AccessToken)).SelectedHeroId);
    }

    [Fact]
    public async Task UnsupportedHero_ReturnsValidationProblem()
    {
        var guest = await CreateGuestAsync();
        using var request = CreateUpdateRequest(guest.AccessToken, "unknown");
        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.True(problem!.Errors.ContainsKey("selectedHeroId"));
    }

    [Fact]
    public async Task UpdatingOnePlayer_DoesNotAffectAnotherPlayer()
    {
        var first = await CreateGuestAsync();
        var second = await CreateGuestAsync();
        await UpdateAsync(first.AccessToken, "black_cat");

        Assert.Equal("black_cat", (await GetStateAsync(_client, first.AccessToken)).SelectedHeroId);
        Assert.Equal("girl", (await GetStateAsync(_client, second.AccessToken)).SelectedHeroId);
    }

    private async Task<GuestAccountResponse> CreateGuestAsync()
    {
        using var response = await _client.PostAsync("/api/v1/accounts/guest", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GuestAccountResponse>())!;
    }

    private async Task<UpdateSelectedHeroResponse> UpdateAsync(string token, string heroId)
    {
        using var request = CreateUpdateRequest(token, heroId);
        using var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UpdateSelectedHeroResponse>())!;
    }

    private static HttpRequestMessage CreateUpdateRequest(string token, string heroId)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, "/api/v1/profile/selected-hero")
        {
            Content = JsonContent.Create(new UpdateSelectedHeroRequest(heroId))
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private static async Task<GameStateResponse> GetStateAsync(HttpClient client, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/game-state");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GameStateResponse>())!;
    }
}
