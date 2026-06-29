using System.Net;
using System.Net.Http.Json;
using IdleGuild.Api.Contracts;

namespace IdleGuild.Api.Tests;

/// <summary>시스템 API의 생존 상태, 시간, 문서 노출을 HTTP 수준에서 검증합니다.</summary>
public sealed class SystemEndpointTests(
    IdleGuildApiFactory factory) : IClassFixture<IdleGuildApiFactory>
{
    // 하나의 테스트 서버에서 실제 요청과 동일한 HttpClient 호출을 수행합니다.
    private readonly HttpClient _client = factory.CreateClient();

    // 운영 모니터링이 서버 생존 여부를 확인할 수 있는지 검증합니다.
    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    // 클라이언트가 로컬 시간이 아닌 서버 UTC 시각을 받을 수 있는지 검증합니다.
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

    // 클라이언트 개발자가 상태 API 계약을 OpenAPI 문서에서 찾을 수 있는지 검증합니다.
    [Fact]
    public async Task OpenApi_DescribesStatusEndpoint()
    {
        var document = await _client.GetStringAsync("/openapi/v1.json");

        Assert.Contains("/api/v1/system/status", document, StringComparison.Ordinal);
    }

    // 개발 중 브라우저에서 API를 직접 시험할 Swagger UI가 제공되는지 검증합니다.
    [Fact]
    public async Task SwaggerUi_IsAvailableInDevelopment()
    {
        var response = await _client.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
