using System.Diagnostics;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace IdleGuild.Api.RateLimiting;

/// <summary>익명 계정 생성과 인증된 상태 변경에 적용할 요청 제한 정책을 구성합니다.</summary>
public static class ApiRateLimitPolicies
{
    public const string GuestAccount = "guest-account";
    public const string PlayerMutation = "player-mutation";
    public const string AdminRead = "admin-read";
    public const int GuestAccountPermitLimit = 5;
    public const int PlayerMutationPermitLimit = 30;
    public const int AdminReadPermitLimit = 120;
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    /// <summary>IP와 JWT 플레이어를 분리 키로 사용하는 고정 윈도우 정책을 등록합니다.</summary>
    public static IServiceCollection AddApiRateLimiting(
        this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode =
                StatusCodes.Status429TooManyRequests;
            options.OnRejected = WriteRejectedResponseAsync;

            options.AddPolicy(
                GuestAccount,
                context => CreateFixedWindowPartition(
                    $"ip:{GetClientAddress(context)}",
                    GuestAccountPermitLimit));
            options.AddPolicy(
                PlayerMutation,
                context => CreateFixedWindowPartition(
                    GetPlayerPartitionKey(context),
                    PlayerMutationPermitLimit));
            options.AddPolicy(
                AdminRead,
                context => CreateFixedWindowPartition(
                    GetPlayerPartitionKey(context),
                    AdminReadPermitLimit));
        });

        return services;
    }

    private static RateLimitPartition<string>
        CreateFixedWindowPartition(
            string partitionKey,
            int permitLimit) =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = permitLimit,
                QueueLimit = 0,
                Window = Window
            });

    private static string GetPlayerPartitionKey(
        HttpContext context)
    {
        var subject = context.User.FindFirst(
            JwtRegisteredClaimNames.Sub)?.Value;

        return Guid.TryParse(subject, out var playerId)
            ? $"player:{playerId:D}"
            : $"unauthenticated-ip:{GetClientAddress(context)}";
    }

    private static string GetClientAddress(
        HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ??
        "unknown";

    private static async ValueTask WriteRejectedResponseAsync(
        OnRejectedContext context,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ??
            context.HttpContext.TraceIdentifier;
        int? retryAfterSeconds = null;

        if (context.Lease.TryGetMetadata(
                MetadataName.RetryAfter,
                out var retryAfter))
        {
            retryAfterSeconds = Math.Max(
                1,
                (int)Math.Ceiling(
                    retryAfter.TotalSeconds));
            context.HttpContext.Response.Headers.RetryAfter =
                retryAfterSeconds.Value.ToString(
                    CultureInfo.InvariantCulture);
        }

        var problemDetails = new ProblemDetails
        {
            Type = "https://httpstatuses.com/429",
            Title = "Too many requests.",
            Status = StatusCodes.Status429TooManyRequests,
            Detail = "Wait before retrying this operation."
        };
        problemDetails.Extensions["traceId"] = traceId;

        if (retryAfterSeconds.HasValue)
        {
            problemDetails.Extensions["retryAfterSeconds"] =
                retryAfterSeconds.Value;
        }

        context.HttpContext.Response.StatusCode =
            StatusCodes.Status429TooManyRequests;
        var problemDetailsService = context.HttpContext
            .RequestServices
            .GetRequiredService<IProblemDetailsService>();
        await problemDetailsService.TryWriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = context.HttpContext,
                ProblemDetails = problemDetails
            });
    }
}
