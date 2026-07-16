using System.Security.Claims;
using IdleGuild.Api.Authentication;
using IdleGuild.Api.Contracts;
using IdleGuild.Api.RateLimiting;
using IdleGuild.Application.Abstractions.Persistence;
using IdleGuild.Application.Profiles.UpdateSelectedHero;
using IdleGuild.Domain.GameStates;

namespace IdleGuild.Api.Endpoints;

/// <summary>인증 플레이어의 서버 저장 프로필 설정 Endpoint를 구성합니다.</summary>
public static class ProfileEndpoints
{
    public static IEndpointRouteBuilder MapProfileEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/profile")
            .WithTags("Profile")
            .RequireAuthorization();

        group.MapPut("/selected-hero", UpdateSelectedHeroAsync)
            .WithName("UpdateSelectedHero")
            .WithSummary("Stores the authenticated player's selected stable hero ID.")
            .RequireRateLimiting(ApiRateLimitPolicies.PlayerMutation)
            .Produces<UpdateSelectedHeroResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return endpoints;
    }

    private static async Task<IResult> UpdateSelectedHeroAsync(
        ClaimsPrincipal user,
        UpdateSelectedHeroRequest request,
        UpdateSelectedHeroHandler handler,
        CancellationToken cancellationToken)
    {
        if (!user.TryGetPlayerId(out var playerId))
        {
            return TypedResults.Unauthorized();
        }

        var selectedHeroId = request.SelectedHeroId?.Trim();
        if (!SelectedHeroPolicy.IsSupported(selectedHeroId))
        {
            return TypedResults.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["selectedHeroId"] =
                    ["Supported values are girl, black_cat, and classic."]
                });
        }

        try
        {
            var saved = await handler.HandleAsync(
                playerId,
                selectedHeroId!,
                cancellationToken);

            return saved is null
                ? EndpointProblemResults.NotFound(
                    "Game state was not found.",
                    "Create a guest account before updating the profile.")
                : TypedResults.Ok(new UpdateSelectedHeroResponse(saved));
        }
        catch (PersistenceConflictException)
        {
            return EndpointProblemResults.ServiceUnavailable(
                "Selected hero update is temporarily busy.",
                "Retry the same selected hero later.");
        }
    }
}
