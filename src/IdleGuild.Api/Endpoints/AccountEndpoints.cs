using IdleGuild.Api.Contracts;
using IdleGuild.Api.RateLimiting;
using IdleGuild.Application.Accounts.CreateGuest;
using Microsoft.AspNetCore.Http.HttpResults;

namespace IdleGuild.Api.Endpoints;

/// <summary>계정 생성과 관련된 HTTP Endpoint를 구성합니다.</summary>
public static class AccountEndpoints
{
    /// <summary>인증 없이 새 게스트를 만드는 API를 등록합니다.</summary>
    public static IEndpointRouteBuilder MapAccountEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/accounts")
            .WithTags("Accounts");

        group
            .MapPost("/guest", CreateGuestAsync)
            .WithName("CreateGuestAccount")
            .WithSummary("Creates a guest account and access token.")
            .AllowAnonymous()
            .RequireRateLimiting(
                ApiRateLimitPolicies.GuestAccount)
            .Produces<GuestAccountResponse>(
                StatusCodes.Status201Created)
            .ProducesProblem(
                StatusCodes.Status429TooManyRequests);

        return endpoints;
    }

    // Endpoint는 HTTP 변환만 담당하고 실제 생성 절차는 Application Handler에 위임합니다.
    private static async Task<Created<GuestAccountResponse>>
        CreateGuestAsync(
            CreateGuestAccountHandler handler,
            CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            cancellationToken);
        var response = new GuestAccountResponse(
            result.PlayerId,
            result.AccessToken,
            result.ExpiresAtUtc);

        return TypedResults.Created(
            "/api/v1/game-state",
            response);
    }
}
