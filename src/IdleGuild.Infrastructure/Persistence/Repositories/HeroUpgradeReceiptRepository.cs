using IdleGuild.Application.Abstractions.Persistence;
using IdleGuild.Domain.Heroes;
using Microsoft.EntityFrameworkCore;

namespace IdleGuild.Infrastructure.Persistence.Repositories;

/// <summary>영웅 강화 영수증 저장소를 EF Core로 구현합니다.</summary>
public sealed class HeroUpgradeReceiptRepository(
    GameDbContext dbContext) :
    IHeroUpgradeReceiptRepository
{
    public Task<HeroUpgradeReceipt?> FindAsync(
        Guid playerId,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        dbContext.HeroUpgradeReceipts
            .AsNoTracking()
            .SingleOrDefaultAsync(
                receipt =>
                    receipt.PlayerId == playerId &&
                    receipt.IdempotencyKey == idempotencyKey,
                cancellationToken);

    /// <summary>새 강화 영수증을 현재 작업 단위에 추가합니다.</summary>
    public void Add(HeroUpgradeReceipt receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        dbContext.HeroUpgradeReceipts.Add(receipt);
    }
}
