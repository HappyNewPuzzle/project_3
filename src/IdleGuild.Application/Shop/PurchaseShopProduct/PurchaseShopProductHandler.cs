using IdleGuild.Application.Abstractions.Persistence;
using IdleGuild.Domain.Economy;
using IdleGuild.Domain.Requests;
using IdleGuild.Domain.Shop;

namespace IdleGuild.Application.Shop.PurchaseShopProduct;

/// <summary>상품 승인, 골드 지급, 구매 영수증과 원장을 한 번만 저장합니다.</summary>
public sealed class PurchaseShopProductHandler(IPlayerGameStateRepository stateRepository,
    IShopPurchaseRepository purchaseRepository, IGoldLedgerRepository ledgerRepository,
    IGameUnitOfWork unitOfWork, TimeProvider timeProvider)
{
    private const int MaxSaveAttempts = 3;

    public async Task<PurchaseShopProductResult?> HandleAsync(Guid playerId, string productId,
        string idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (playerId == Guid.Empty) throw new ArgumentException("Player ID must not be empty.", nameof(playerId));
        ArgumentException.ThrowIfNullOrWhiteSpace(productId);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        var normalizedProductId = productId.Trim();
        var normalizedKey = idempotencyKey.Trim();
        if (normalizedKey.Length > IdempotencyPolicy.MaxKeyLength) throw new ArgumentOutOfRangeException(nameof(idempotencyKey));
        var product = ShopCatalog.Find(normalizedProductId);
        if (product is null) return null;
        var now = timeProvider.GetUtcNow();

        for (var attempt = 1; attempt <= MaxSaveAttempts; attempt++)
        {
            var existing = await purchaseRepository.FindAsync(playerId, normalizedKey, cancellationToken);
            if (existing is not null)
            {
                if (existing.ProductId != normalizedProductId)
                    throw new IdempotencyKeyConflictException("Idempotency key was already used for a different product.");
                return FromReceipt(existing, true);
            }

            var state = await stateRepository.FindForUpdateAsync(playerId, cancellationToken);
            if (state is null) return null;
            var before = state.Gold;
            var after = state.GrantShopGold(product.GoldAwarded);
            var receipt = ShopPurchaseReceipt.Create(playerId, normalizedKey, product, after, now);
            purchaseRepository.Add(receipt);
            ledgerRepository.Add(GoldLedgerEntry.Create(playerId, GoldLedgerReason.ShopPurchase,
                before, product.GoldAwarded, after, normalizedKey, now));

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return FromReceipt(receipt, false);
            }
            catch (PersistenceConflictException) when (attempt < MaxSaveAttempts)
            {
                unitOfWork.DiscardChanges();
            }
        }

        throw new PersistenceConflictException(
            "Shop purchase could not be saved after retries.",
            new InvalidOperationException("Persistence retry limit was exceeded."));
    }

    private static PurchaseShopProductResult FromReceipt(ShopPurchaseReceipt receipt, bool replay) =>
        new(receipt.PurchaseId, receipt.IdempotencyKey, receipt.ProductId, receipt.MockPrice,
            receipt.GoldAwarded, receipt.GoldBalanceAfter, receipt.PurchasedAtUtc, replay);
}
