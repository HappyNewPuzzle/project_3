using System.Security.Claims;
using IdleGuild.Api.Authentication;
using IdleGuild.Api.Contracts;
using IdleGuild.Api.RateLimiting;
using IdleGuild.Application.Abstractions.Persistence;
using IdleGuild.Application.Equipment.ChangeEquipment;
using IdleGuild.Application.Equipment.GetEquipment;
using IdleGuild.Domain.Equipment;

namespace IdleGuild.Api.Endpoints;

/// <summary>인증된 플레이어의 장비 조회와 장착 상태 변경 Endpoint를 구성합니다.</summary>
public static class EquipmentEndpoints
{
    public static IEndpointRouteBuilder MapEquipmentEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/equipment")
            .WithTags("Equipment")
            .RequireAuthorization();

        group.MapGet("/", GetEquipmentAsync)
            .WithName("GetEquipment")
            .WithSummary(
                "Returns owned equipment and the equipped power bonus.")
            .Produces<EquipmentInventoryResponse>()
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{equipmentId:guid}/equipped",
                ChangeEquipmentAsync)
            .WithName("ChangeEquipment")
            .WithSummary(
                "Equips or unequips one owned item exactly once per idempotency key.")
            .RequireRateLimiting(
                ApiRateLimitPolicies.PlayerMutation)
            .Produces<ChangeEquipmentResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return endpoints;
    }

    private static async Task<IResult> GetEquipmentAsync(
        ClaimsPrincipal user,
        GetEquipmentHandler handler,
        CancellationToken cancellationToken)
    {
        if (!user.TryGetPlayerId(out var playerId))
        {
            return TypedResults.Unauthorized();
        }

        var result = await handler.HandleAsync(
            playerId,
            cancellationToken);

        if (result is null)
        {
            return PlayerNotFound();
        }

        return TypedResults.Ok(
            new EquipmentInventoryResponse(
                result.PlayerId,
                result.EquipmentPowerBonus,
                result.Items.Select(item =>
                    new EquipmentItemResponse(
                        item.EquipmentId,
                        item.DefinitionId,
                        item.Name,
                        item.Slot,
                        item.PowerBonus,
                        item.IsEquipped,
                        item.AcquiredAtUtc))
                    .ToArray()));
    }

    private static async Task<IResult> ChangeEquipmentAsync(
        Guid equipmentId,
        ChangeEquipmentRequest requestBody,
        ClaimsPrincipal user,
        HttpRequest request,
        ChangeEquipmentHandler handler,
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

        ChangeEquipmentResult? result;

        try
        {
            result = await handler.HandleAsync(
                playerId,
                equipmentId,
                requestBody.IsEquipped,
                idempotencyKey,
                cancellationToken);
        }
        catch (IdempotencyKeyConflictException exception)
        {
            return EndpointProblemResults.Conflict(
                "Idempotency key conflict.",
                exception.Message);
        }
        catch (PersistenceConflictException)
        {
            return EndpointProblemResults.ServiceUnavailable(
                "Equipment change is temporarily busy.",
                "Retry later with the same Idempotency-Key and body.");
        }

        return result is null
            ? EndpointProblemResults.NotFound(
                "Equipment was not found.",
                "The equipment is not owned by the authenticated player.")
            : TypedResults.Ok(new ChangeEquipmentResponse(
                result.IdempotencyKey,
                result.EquipmentId,
                result.IsEquipped,
                result.Outcome ==
                    EquipmentChangeOutcome.Succeeded
                    ? "succeeded"
                    : "alreadyInDesiredState",
                result.ReplacedEquipmentId,
                result.ProcessedAtUtc,
                result.IsReplay));
    }

    private static IResult PlayerNotFound() =>
        EndpointProblemResults.NotFound(
            "Game state was not found.",
            "Create a guest account before calling this endpoint.");
}
