using System.Security.Claims;
using IdleGuild.Api.Authentication;
using IdleGuild.Api.Contracts;
using IdleGuild.Application.GameStates.GetGameState;
using IdleGuild.Application.GameStates.SyncProgression;
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

        group.MapPut("/progression", SyncProgressionAsync)
            .WithName("SyncProgression")
            .WithSummary("Merges durable client progression into the authenticated player's state.")
            .Produces<SyncProgressionRequest>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> SyncProgressionAsync(
        ClaimsPrincipal user,
        SyncProgressionRequest request,
        SyncProgressionHandler handler,
        CancellationToken cancellationToken)
    {
        if (!user.TryGetPlayerId(out var playerId)) return TypedResults.Unauthorized();
        if (request.AttackLevel < 1 || request.AttackSpeedLevel < 0 || request.CriticalLevel < 0 || request.PrestigeLevel < 0 || request.SoulStones < 0 ||
            request.EquipmentTier < 0 || request.EquipmentCount < 0 || request.UnlockedRegion < 0 || request.SkillOneLevel < 1 || request.SkillTwoLevel < 1 || request.SkillThreeLevel < 1)
            return TypedResults.BadRequest();

        bool found = await handler.HandleAsync(
            playerId,
            request.AttackLevel,
            request.AttackSpeedLevel,
            request.CriticalLevel,
            request.PrestigeLevel,
            request.SoulStones,
            request.EquipmentTier,
            request.EquipmentCount,
            request.UnlockedRegion,
            request.SkillOneLevel,
            request.SkillTwoLevel,
            request.SkillThreeLevel,
            cancellationToken);
        return found ? TypedResults.Ok(request) : TypedResults.NotFound();
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
                result.AttackLevel,
                result.AttackSpeedLevel,
                result.CriticalLevel,
                result.PrestigeLevel,
                result.SoulStones,
                result.EquipmentTier,
                result.EquipmentCount,
                result.UnlockedRegion,
                result.SkillOneLevel,
                result.SkillTwoLevel,
                result.SkillThreeLevel,
                result.ProductionBonusPercent,
                result.IdleRewardRemainderHundredths,
                result.LastIdleRewardClaimedAtUtc));
    }
}
