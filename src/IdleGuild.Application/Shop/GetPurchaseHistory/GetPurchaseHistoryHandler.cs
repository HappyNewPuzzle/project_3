using IdleGuild.Application.Abstractions.Persistence;
using IdleGuild.Domain.Shop;

namespace IdleGuild.Application.Shop.GetPurchaseHistory;

/// <summary>인증된 플레이어의 모의 구매 이력을 최신순으로 조회합니다.</summary>
public sealed class GetPurchaseHistoryHandler(IPlayerGameStateRepository stateRepository,
    IShopPurchaseRepository purchaseRepository)
{
    public async Task<IReadOnlyList<ShopPurchaseReceipt>?> HandleAsync(Guid playerId,
        CancellationToken cancellationToken = default) =>
        await stateRepository.FindByIdAsync(playerId, cancellationToken) is null
            ? null
            : await purchaseRepository.ListAsync(playerId, cancellationToken);
}
