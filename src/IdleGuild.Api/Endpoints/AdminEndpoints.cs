using IdleGuild.Api.Authorization;
using IdleGuild.Api.Contracts;
using IdleGuild.Api.RateLimiting;
using IdleGuild.Application.Admin.Players.GetAdminPlayer;
using IdleGuild.Application.Admin.Players.GetGoldLedgerPage;
using IdleGuild.Domain.Economy;

namespace IdleGuild.Api.Endpoints;

/// <summary>관리자 권한으로 플레이어 상태와 골드 감사 이력을 조회하는 Endpoint를 구성합니다.</summary>
public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/admin")
            .WithTags("Admin")
            .RequireAuthorization(
                AdminAuthorization.PolicyName)
            .RequireRateLimiting(
                ApiRateLimitPolicies.AdminRead);

        group
            .MapGet("/players/{playerId:guid}",
                GetPlayerAsync)
            .WithName("GetAdminPlayer")
            .WithSummary(
                "Reads one player's current server-authoritative state.")
            .Produces<AdminPlayerResponse>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status429TooManyRequests);

        group
            .MapGet("/players/{playerId:guid}/gold-ledger",
                GetGoldLedgerAsync)
            .WithName("GetAdminPlayerGoldLedger")
            .WithSummary(
                "Reads a cursor-paginated page of one player's gold ledger.")
            .Produces<AdminGoldLedgerPageResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status429TooManyRequests);

        return endpoints;
    }

    private static async Task<IResult> GetPlayerAsync(
        Guid playerId,
        GetAdminPlayerHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            playerId,
            cancellationToken);

        return result is null
            ? PlayerNotFound()
            : TypedResults.Ok(new AdminPlayerResponse(
                result.PlayerId,
                result.Gold,
                result.HeroLevel,
                result.HighestStage,
                result.ProductionBonusPercent,
                result.IdleRewardRemainderHundredths,
                result.CreatedAtUtc,
                result.LastIdleRewardClaimedAtUtc,
                result.Version));
    }

    private static async Task<IResult> GetGoldLedgerAsync(
        Guid playerId,
        int? pageSize,
        string? cursor,
        GetGoldLedgerPageHandler handler,
        CancellationToken cancellationToken)
    {
        var requestedPageSize = pageSize ??
            GetGoldLedgerPageHandler.DefaultPageSize;

        if (requestedPageSize < 1 ||
            requestedPageSize >
            GetGoldLedgerPageHandler.MaxPageSize)
        {
            return EndpointProblemResults.BadRequest(
                "Page size is outside the supported range.",
                $"pageSize must be between 1 and {GetGoldLedgerPageHandler.MaxPageSize}.");
        }

        if (!AdminLedgerCursorCodec.TryDecode(
                cursor,
                out var position))
        {
            return EndpointProblemResults.BadRequest(
                "Ledger cursor is invalid.",
                "Use the nextCursor value returned by the previous response.");
        }

        var result = await handler.HandleAsync(
            playerId,
            requestedPageSize,
            position,
            cancellationToken);

        if (result is null)
        {
            return PlayerNotFound();
        }

        var items = result.Items
            .Select(item =>
                new AdminGoldLedgerEntryResponse(
                    item.EntryId,
                    ToApiValue(item.Reason),
                    item.BalanceBefore,
                    item.Amount,
                    item.BalanceAfter,
                    item.ReferenceId,
                    item.OccurredAtUtc))
            .ToArray();
        var nextCursor = result.NextPosition is null
            ? null
            : AdminLedgerCursorCodec.Encode(
                result.NextPosition);

        return TypedResults.Ok(
            new AdminGoldLedgerPageResponse(
                result.PlayerId,
                items,
                nextCursor));
    }

    private static IResult PlayerNotFound() =>
        EndpointProblemResults.NotFound(
            "Player was not found.",
            "No game state exists for the requested player ID.");

    private static string ToApiValue(
        GoldLedgerReason reason) =>
        reason switch
        {
            GoldLedgerReason.IdleRewardClaim =>
                "idleRewardClaim",
            GoldLedgerReason.HeroUpgrade =>
                "heroUpgrade",
            GoldLedgerReason.StageCheckpoint =>
                "stageCheckpoint",
            _ => throw new ArgumentOutOfRangeException(
                nameof(reason))
        };
}
