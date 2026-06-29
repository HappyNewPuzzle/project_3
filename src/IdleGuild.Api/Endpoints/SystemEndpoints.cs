using IdleGuild.Api.Contracts;

namespace IdleGuild.Api.Endpoints;

/// <summary>시스템 상태와 관련된 HTTP 엔드포인트를 한곳에서 구성합니다.</summary>
public static class SystemEndpoints
{
    /// <summary>버전이 지정된 시스템 API 경로를 애플리케이션에 등록합니다.</summary>
    public static IEndpointRouteBuilder MapSystemEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/system")
            .WithTags("System");

        group
            .MapGet("/status", (TimeProvider timeProvider) =>
                TypedResults.Ok(new SystemStatusResponse(
                    "ok",
                    timeProvider.GetUtcNow())))
            .WithName("GetSystemStatus")
            .WithSummary("Returns the API status and current server UTC time.")
            .Produces<SystemStatusResponse>();

        return endpoints;
    }
}
