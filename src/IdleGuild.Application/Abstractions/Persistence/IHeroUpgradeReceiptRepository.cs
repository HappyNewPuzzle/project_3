using IdleGuild.Domain.Heroes;

namespace IdleGuild.Application.Abstractions.Persistence;

/// <summary>영웅 강화 영수증의 조회와 추가를 저장 기술과 분리합니다.</summary>
public interface IHeroUpgradeReceiptRepository
{
    Task<HeroUpgradeReceipt?> FindAsync(
        Guid playerId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    void Add(HeroUpgradeReceipt receipt);
}
