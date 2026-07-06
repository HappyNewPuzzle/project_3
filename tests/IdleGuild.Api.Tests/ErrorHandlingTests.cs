using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IdleGuild.Api.Contracts;
using IdleGuild.Application.Abstractions.Persistence;
using IdleGuild.Domain.GameStates;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IdleGuild.Api.Tests;

/// <summary>예상하지 못한 서버 예외가 안전한 HTTP 오류 계약으로 변환되는지 검증합니다.</summary>
public sealed class ErrorHandlingTests(
    IdleGuildApiFactory factory) :
    IClassFixture<IdleGuildApiFactory>
{
    // 전역 예외 처리 미들웨어가 예외 메시지 대신 추적 가능한 ProblemDetails를 반환해야 합니다.
    [Fact]
    public async Task UnhandledException_ReturnsProblemDetails()
    {
        var guest = await CreateGuestAsync();
        using var client = CreateThrowingClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/v1/game-state");
        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                guest.AccessToken);

        var response = await client.SendAsync(request);
        var problem = await response.Content
            .ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);
        Assert.Equal(
            "An unexpected server error occurred.",
            problem?.Title);
        Assert.Equal(500, problem?.Status);
        Assert.True(
            problem?.Extensions.ContainsKey("traceId"));
    }

    private async Task<GuestAccountResponse>
        CreateGuestAsync()
    {
        using var client = factory.CreateClient();
        var response = await client.PostAsync(
            "/api/v1/accounts/guest",
            content: null);
        var guest = await response.Content
            .ReadFromJsonAsync<GuestAccountResponse>();

        return Assert.IsType<GuestAccountResponse>(guest);
    }

    private HttpClient CreateThrowingClient() =>
        factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IPlayerGameStateRepository>();
                services.AddSingleton<IPlayerGameStateRepository,
                    ThrowingPlayerGameStateRepository>();
            });
        }).CreateClient();

    private sealed class ThrowingPlayerGameStateRepository :
        IPlayerGameStateRepository
    {
        public void Add(PlayerGameState gameState) =>
            throw new InvalidOperationException(
                "Forced repository failure for testing.");

        public Task<PlayerGameState?> FindByIdAsync(
            Guid playerId,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException(
                "Forced repository failure for testing.");

        public Task<PlayerGameState?> FindForUpdateAsync(
            Guid playerId,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException(
                "Forced repository failure for testing.");
    }
}
