using System.Security.Claims;
using IdleGuild.Api.Authentication;
using IdleGuild.Api.Contracts;
using IdleGuild.Application.GameStates.GetGameState;
using Microsoft.AspNetCore.Http.HttpResults;

namespace IdleGuild.Api.Endpoints;

/// <summary>인증된 플레이어의 게임 상태 HTTP Endpoint를 구성합니다.</summary>
public static class GameStateEndpoints
{
    /// <summary>JWT subject에 해당하는 상태만 조회하는 API를 등록합니다.</summary>
    public static IEndpointRouteBuilder MapGameStateEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/game-state")
            .WithTags("Game State")
            .RequireAuthorization();

        group
            .MapGet("/", GetGameStateAsync)
            .WithName("GetGameState")
            .WithSummary(
                "Returns the authenticated player's game state.")
            .Produces<GameStateResponse>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }

    // 요청 경로나 Body에서 플레이어 ID를 받지 않아 다른 사용자를 지정할 수 없게 합니다.
    private static async Task<Results<
        Ok<GameStateResponse>,
        UnauthorizedHttpResult,
        NotFound>> GetGameStateAsync(
        ClaimsPrincipal user,
        GetGameStateHandler handler,
        CancellationToken cancellationToken)
    {
        if (!user.TryGetPlayerId(out var playerId))
        {
            return TypedResults.Unauthorized();
        }

        var result = await handler.HandleAsync(
            playerId,
            cancellationToken);

        return result is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(new GameStateResponse(
                result.PlayerId,
                result.Gold,
                result.HeroLevel,
                result.HeroPower,
                result.EquipmentPowerBonus,
                result.HighestStage,
                result.ProductionBonusPercent,
                result.IdleRewardRemainderHundredths,
                result.LastIdleRewardClaimedAtUtc));
    }
}
