using System.Security.Claims;
using IdleGuild.Api.Authentication;
using IdleGuild.Api.Contracts;
using IdleGuild.Application.Abstractions.Persistence;
using IdleGuild.Application.Rewards.ClaimIdleReward;

namespace IdleGuild.Api.Endpoints;

/// <summary>인증된 플레이어의 보상 지급 HTTP Endpoint를 구성합니다.</summary>
public static class RewardsEndpoints
{
    /// <summary>중복 요청에도 한 번만 지급하는 방치 보상 API를 등록합니다.</summary>
    public static IEndpointRouteBuilder MapRewardsEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/rewards")
            .WithTags("Rewards")
            .RequireAuthorization();

        group
            .MapPost("/idle/claim", ClaimIdleRewardAsync)
            .WithName("ClaimIdleReward")
            .WithSummary(
                "Claims accumulated idle rewards exactly once per idempotency key.")
            .Produces<IdleRewardClaimResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return endpoints;
    }

    // JWT의 플레이어와 요청 헤더의 멱등 키만 사용해 보상을 정산합니다.
    private static async Task<IResult> ClaimIdleRewardAsync(
        ClaimsPrincipal user,
        HttpRequest request,
        ClaimIdleRewardHandler handler,
        CancellationToken cancellationToken)
    {
        if (!user.TryGetPlayerId(out var playerId))
        {
            return TypedResults.Unauthorized();
        }

        if (!EndpointProblemResults.TryReadIdempotencyKey(
                request,
                out var idempotencyKey,
                out var problem))
        {
            return problem!;
        }

        ClaimIdleRewardResult? result;

        try
        {
            result = await handler.HandleAsync(
                playerId,
                idempotencyKey,
                cancellationToken);
        }
        catch (PersistenceConflictException)
        {
            // 짧은 재시도 후에도 충돌하면 클라이언트가 같은 키로 다시 요청하게 알립니다.
            return EndpointProblemResults.ServiceUnavailable(
                "Idle reward claim is temporarily busy.",
                "Retry later with the same Idempotency-Key.");
        }

        return result is null
            ? EndpointProblemResults.NotFound(
                "Game state was not found.",
                "Create a guest account before calling this endpoint.")
            : TypedResults.Ok(new IdleRewardClaimResponse(
                result.IdempotencyKey,
                result.GoldAwarded,
                result.AccumulatedSeconds,
                result.GoldBalanceAfter,
                result.RemainderHundredths,
                result.ProductionPercent,
                result.ClaimedAtUtc,
                result.IsReplay));
    }
}
