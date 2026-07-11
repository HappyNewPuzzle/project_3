using System.Security.Claims;
using IdleGuild.Api.Authentication;
using IdleGuild.Api.Contracts;
using IdleGuild.Api.RateLimiting;
using IdleGuild.Application.Abstractions.Persistence;
using IdleGuild.Application.Shop.GetPurchaseHistory;
using IdleGuild.Application.Shop.GetShopCatalog;
using IdleGuild.Application.Shop.PurchaseShopProduct;

namespace IdleGuild.Api.Endpoints;

/// <summary>모의 상품 조회, 구매와 구매 이력 Endpoint를 구성합니다.</summary>
public static class ShopEndpoints
{
    public static IEndpointRouteBuilder MapShopEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/shop").WithTags("Shop").RequireAuthorization();
        group.MapGet("/products", GetProducts).WithName("GetShopProducts").Produces<ShopCatalogResponse>();
        group.MapPost("/products/{productId}/purchase", PurchaseAsync).WithName("PurchaseShopProduct")
            .RequireRateLimiting(ApiRateLimitPolicies.PlayerMutation)
            .Produces<ShopPurchaseResponse>().ProducesProblem(400).Produces(401).ProducesProblem(404)
            .ProducesProblem(409).ProducesProblem(429).ProducesProblem(503);
        group.MapGet("/purchases", GetPurchasesAsync).WithName("GetShopPurchases")
            .Produces<ShopPurchaseHistoryResponse>().Produces(401).ProducesProblem(404);
        return endpoints;
    }

    private static IResult GetProducts(GetShopCatalogHandler handler) =>
        TypedResults.Ok(new ShopCatalogResponse(handler.Handle().Select(product =>
            new ShopProductResponse(product.ProductId, product.Name, product.MockPrice, product.GoldAwarded)).ToArray()));

    private static async Task<IResult> PurchaseAsync(string productId, ClaimsPrincipal user, HttpRequest request,
        PurchaseShopProductHandler handler, CancellationToken cancellationToken)
    {
        if (!user.TryGetPlayerId(out var playerId)) return TypedResults.Unauthorized();
        if (!EndpointProblemResults.TryReadIdempotencyKey(request, out var key, out var problem)) return problem!;
        try
        {
            var result = await handler.HandleAsync(playerId, productId, key, cancellationToken);
            return result is null
                ? EndpointProblemResults.NotFound("Shop product or player was not found.", "Use a product ID returned by the catalog.")
                : TypedResults.Ok(ToResponse(result));
        }
        catch (IdempotencyKeyConflictException exception)
        {
            return EndpointProblemResults.Conflict("Idempotency key conflict.", exception.Message);
        }
        catch (PersistenceConflictException)
        {
            return EndpointProblemResults.ServiceUnavailable("Purchase is temporarily busy.", "Retry with the same Idempotency-Key and product.");
        }
    }

    private static async Task<IResult> GetPurchasesAsync(ClaimsPrincipal user, GetPurchaseHistoryHandler handler,
        CancellationToken cancellationToken)
    {
        if (!user.TryGetPlayerId(out var playerId)) return TypedResults.Unauthorized();
        var receipts = await handler.HandleAsync(playerId, cancellationToken);
        return receipts is null
            ? EndpointProblemResults.NotFound("Game state was not found.", "Create a guest account first.")
            : TypedResults.Ok(new ShopPurchaseHistoryResponse(receipts.Select(receipt =>
                new ShopPurchaseResponse(receipt.PurchaseId, receipt.IdempotencyKey, receipt.ProductId,
                    receipt.MockPrice, receipt.GoldAwarded, receipt.GoldBalanceAfter,
                    receipt.PurchasedAtUtc, false)).ToArray()));
    }

    private static ShopPurchaseResponse ToResponse(PurchaseShopProductResult result) =>
        new(result.PurchaseId, result.IdempotencyKey, result.ProductId, result.MockPrice,
            result.GoldAwarded, result.GoldBalanceAfter, result.PurchasedAtUtc, result.IsReplay);
}
