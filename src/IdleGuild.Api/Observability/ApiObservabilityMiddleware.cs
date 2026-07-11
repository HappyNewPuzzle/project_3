using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Routing;

namespace IdleGuild.Api.Observability;

/// <summary>낮은 카디널리티 메트릭, 구조화 완료 로그와 Trace ID 응답을 생성합니다.</summary>
public sealed class ApiObservabilityMiddleware(
    RequestDelegate next,
    ILogger<ApiObservabilityMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var started = Stopwatch.GetTimestamp();
        var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[ApiTelemetry.TraceHeaderName] = traceId;
            return Task.CompletedTask;
        });

        await next(context);

        var elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        var route = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "unmatched";
        var method = context.Request.Method;
        var statusCode = context.Response.StatusCode;
        var statusClass = $"{statusCode / 100}xx";
        var tags = new TagList
        {
            { "http.request.method", method },
            { "http.route", route },
            { "http.response.status_class", statusClass }
        };

        ApiTelemetry.RequestCount.Add(1, tags);
        ApiTelemetry.RequestDuration.Record(elapsedMs, tags);
        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            ApiTelemetry.ErrorCount.Add(1, tags);
        }

        var playerId = context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        logger.LogInformation(
            "HTTP request completed TraceId={TraceId} Method={Method} Route={Route} StatusCode={StatusCode} DurationMs={DurationMs} PlayerId={PlayerId}",
            traceId,
            method,
            route,
            statusCode,
            Math.Round(elapsedMs, 2),
            playerId ?? "anonymous");
    }
}
