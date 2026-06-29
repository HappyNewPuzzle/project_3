using IdleGuild.Api.Contracts;

namespace IdleGuild.Api.Endpoints;

public static class SystemEndpoints
{
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
