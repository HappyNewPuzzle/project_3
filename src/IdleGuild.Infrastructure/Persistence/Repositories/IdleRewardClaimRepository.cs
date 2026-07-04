using IdleGuild.Application.Abstractions.Persistence;
using IdleGuild.Domain.Rewards;
using Microsoft.EntityFrameworkCore;

namespace IdleGuild.Infrastructure.Persistence.Repositories;

/// <summary>방치 보상 영수증 저장소를 EF Core로 구현합니다.</summary>
public sealed class IdleRewardClaimRepository(
    GameDbContext dbContext) : IIdleRewardClaimRepository
{
    public Task<IdleRewardClaimReceipt?> FindAsync(
        Guid playerId,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        dbContext.IdleRewardClaimReceipts
            .AsNoTracking()
            .SingleOrDefaultAsync(
                receipt =>
                    receipt.PlayerId == playerId &&
                    receipt.IdempotencyKey == idempotencyKey,
                cancellationToken);

    /// <summary>새 정산 영수증을 현재 작업 단위에 추가합니다.</summary>
    public void Add(IdleRewardClaimReceipt receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        dbContext.IdleRewardClaimReceipts.Add(receipt);
    }
}
