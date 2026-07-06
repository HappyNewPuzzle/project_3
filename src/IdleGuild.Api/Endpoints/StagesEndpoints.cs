using System.Security.Claims;
using IdleGuild.Api.Authentication;
using IdleGuild.Api.Contracts;
using IdleGuild.Application.Abstractions.Persistence;
using IdleGuild.Application.Stages.ChallengeStage;
using IdleGuild.Domain.Requests;
using IdleGuild.Domain.Stages;
using Microsoft.Extensions.Primitives;

namespace IdleGuild.Api.Endpoints;

/// <summary>인증된 플레이어의 스테이지 진행 HTTP Endpoint를 구성합니다.</summary>
public static class StagesEndpoints
{
    /// <summary>결정론적 전투력으로 스테이지를 도전하는 API를 등록합니다.</summary>
    public static IEndpointRouteBuilder MapStagesEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/stages")
            .WithTags("Stages")
            .RequireAuthorization();

        group
            .MapPost("/{stage:int}/challenge",
                ChallengeStageAsync)
            .WithName("ChallengeStage")
            .WithSummary(
                "Deterministically challenges the next stage once per idempotency key.")
            .Produces<StageChallengeResponse>()
            .Produces<StageChallengeResponse>(
                StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return endpoints;
    }

    // 경로의 목표 스테이지와 인증 사용자로 서버 권위 판정을 요청합니다.
    private static async Task<IResult> ChallengeStageAsync(
        int stage,
        ClaimsPrincipal user,
        HttpRequest request,
        ChallengeStageHandler handler,
        CancellationToken cancellationToken)
    {
        if (!user.TryGetPlayerId(out var playerId))
        {
            return TypedResults.Unauthorized();
        }

        if (stage < 1 ||
            stage > StageChallengePolicy.MaxStage)
        {
            return TypedResults.BadRequest(
                $"Stage must be between 1 and {StageChallengePolicy.MaxStage}.");
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

        ChallengeStageResult? result;

        try
        {
            result = await handler.HandleAsync(
                playerId,
                stage,
                idempotencyKey,
                cancellationToken);
        }
        catch (IdempotencyKeyConflictException exception)
        {
            return TypedResults.Problem(
                title: "Idempotency key conflict.",
                detail: exception.Message,
                statusCode: StatusCodes.Status409Conflict);
        }
        catch (PersistenceConflictException)
        {
            // 내부 재시도 후에도 충돌하면 같은 키와 스테이지로 다시 요청하게 합니다.
            return TypedResults.Problem(
                title:
                    "Stage challenge is temporarily busy.",
                detail:
                    "Retry later with the same Idempotency-Key and stage.",
                statusCode:
                    StatusCodes.Status503ServiceUnavailable);
        }

        if (result is null)
        {
            return TypedResults.NotFound();
        }

        var response = new StageChallengeResponse(
            result.IdempotencyKey,
            result.TargetStage,
            ToApiValue(result.Outcome),
            result.PreviousHighestStage,
            result.HighestStageAfter,
            result.HeroPower,
            result.RequiredPower,
            result.ProductionBonusPercentAfter,
            result.CheckpointGoldAwarded,
            result.GoldBalanceAfter,
            result.ProcessedAtUtc,
            result.IsReplay);

        return result.Outcome ==
            StageChallengeOutcome.Succeeded
            ? TypedResults.Ok(response)
            : TypedResults.Conflict(response);
    }

    private static string ToApiValue(
        StageChallengeOutcome outcome) =>
        outcome switch
        {
            StageChallengeOutcome.Succeeded =>
                "succeeded",
            StageChallengeOutcome.InsufficientPower =>
                "insufficientPower",
            StageChallengeOutcome.AlreadyCompleted =>
                "alreadyCompleted",
            StageChallengeOutcome.StageLocked =>
                "stageLocked",
            _ => throw new ArgumentOutOfRangeException(
                nameof(outcome))
        };
}
