using System.Security.Claims;
using IdleGuild.Api.Authentication;
using IdleGuild.Api.Contracts;
using IdleGuild.Application.Abstractions.Persistence;
using IdleGuild.Application.Heroes.UpgradeMainHero;
using IdleGuild.Domain.Heroes;
using IdleGuild.Domain.Requests;
using Microsoft.Extensions.Primitives;

namespace IdleGuild.Api.Endpoints;

/// <summary>인증된 플레이어의 영웅 성장 HTTP Endpoint를 구성합니다.</summary>
public static class HeroesEndpoints
{
    /// <summary>골드를 소비해 주 영웅을 한 레벨 강화하는 API를 등록합니다.</summary>
    public static IEndpointRouteBuilder MapHeroesEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/heroes")
            .WithTags("Heroes")
            .RequireAuthorization();

        group
            .MapPost("/main/upgrade", UpgradeMainHeroAsync)
            .WithName("UpgradeMainHero")
            .WithSummary(
                "Spends gold to upgrade the main hero exactly once per idempotency key.")
            .Produces<HeroUpgradeResponse>()
            .Produces<HeroUpgradeResponse>(
                StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return endpoints;
    }

    // 인증 사용자와 멱등 키만 받아 비용과 결과 계산은 Application·Domain에 맡깁니다.
    private static async Task<IResult> UpgradeMainHeroAsync(
        ClaimsPrincipal user,
        HttpRequest request,
        UpgradeMainHeroHandler handler,
        CancellationToken cancellationToken)
    {
        if (!user.TryGetPlayerId(out var playerId))
        {
            return TypedResults.Unauthorized();
        }

        if (!request.Headers.TryGetValue(
                "Idempotency-Key",
                out StringValues headerValue) ||
            headerValue.Count != 1 ||
            string.IsNullOrWhiteSpace(headerValue[0]))
        {
            return TypedResults.BadRequest(
                "Idempotency-Key header is required.");
        }

        var idempotencyKey = headerValue[0]!.Trim();

        if (idempotencyKey.Length >
            IdempotencyPolicy.MaxKeyLength)
        {
            return TypedResults.BadRequest(
                $"Idempotency-Key cannot exceed {IdempotencyPolicy.MaxKeyLength} characters.");
        }

        UpgradeMainHeroResult? result;

        try
        {
            result = await handler.HandleAsync(
                playerId,
                idempotencyKey,
                cancellationToken);
        }
        catch (PersistenceConflictException)
        {
            // 내부 재시도 후에도 충돌하면 같은 키로 다시 시도할 수 있는 오류를 반환합니다.
            return TypedResults.Problem(
                title: "Hero upgrade is temporarily busy.",
                detail:
                    "Retry later with the same Idempotency-Key.",
                statusCode:
                    StatusCodes.Status503ServiceUnavailable);
        }

        if (result is null)
        {
            return TypedResults.NotFound();
        }

        var response = new HeroUpgradeResponse(
            result.IdempotencyKey,
            ToApiValue(result.Outcome),
            result.PreviousLevel,
            result.HeroLevelAfter,
            result.GoldCost,
            result.GoldBalanceAfter,
            result.ProcessedAtUtc,
            result.IsReplay);

        return result.Outcome ==
            HeroUpgradeOutcome.Succeeded
            ? TypedResults.Ok(response)
            : TypedResults.Conflict(response);
    }

    private static string ToApiValue(
        HeroUpgradeOutcome outcome) =>
        outcome switch
        {
            HeroUpgradeOutcome.Succeeded =>
                "succeeded",
            HeroUpgradeOutcome.InsufficientGold =>
                "insufficientGold",
            HeroUpgradeOutcome.MaxLevelReached =>
                "maxLevelReached",
            _ => throw new ArgumentOutOfRangeException(
                nameof(outcome))
        };
}
