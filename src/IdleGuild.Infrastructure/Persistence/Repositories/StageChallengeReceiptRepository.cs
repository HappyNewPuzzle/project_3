using IdleGuild.Application.Abstractions.Persistence;
using IdleGuild.Domain.Stages;
using Microsoft.EntityFrameworkCore;

namespace IdleGuild.Infrastructure.Persistence.Repositories;

/// <summary>스테이지 도전 영수증 저장소를 EF Core로 구현합니다.</summary>
public sealed class StageChallengeReceiptRepository(
    GameDbContext dbContext) :
    IStageChallengeReceiptRepository
{
    public Task<StageChallengeReceipt?> FindAsync(
        Guid playerId,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        dbContext.StageChallengeReceipts
            .AsNoTracking()
            .SingleOrDefaultAsync(
                receipt =>
                    receipt.PlayerId == playerId &&
                    receipt.IdempotencyKey == idempotencyKey,
                cancellationToken);

    /// <summary>새 스테이지 도전 영수증을 현재 작업 단위에 추가합니다.</summary>
    public void Add(StageChallengeReceipt receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        dbContext.StageChallengeReceipts.Add(receipt);
    }
}
