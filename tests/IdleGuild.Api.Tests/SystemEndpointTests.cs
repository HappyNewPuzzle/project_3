using System.Net;
using System.Net.Http.Json;
using IdleGuild.Api.Contracts;

namespace IdleGuild.Api.Tests;

public sealed class SystemEndpointTests(
    IdleGuildApiFactory factory) : IClassFixture<IdleGuildApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Status_ReturnsServerUtcTime()
    {
        var beforeRequest = DateTimeOffset.UtcNow;

        var status = await _client.GetFromJsonAsync<SystemStatusResponse>(
            "/api/v1/system/status");

        Assert.NotNull(status);
        Assert.Equal("ok", status.Status);
        Assert.InRange(
            status.ServerTimeUtc,
            beforeRequest,
            DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task OpenApi_DescribesStatusEndpoint()
    {
        var document = await _client.GetStringAsync("/openapi/v1.json");

        Assert.Contains("/api/v1/system/status", document, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SwaggerUi_IsAvailableInDevelopment()
    {
        var response = await _client.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
