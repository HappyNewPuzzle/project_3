using IdleGuild.Application.Abstractions.Persistence;
using IdleGuild.Domain.Equipment;
using Microsoft.EntityFrameworkCore;

namespace IdleGuild.Infrastructure.Persistence.Repositories;

/// <summary>장비 변경 영수증 조회와 추가를 EF Core로 구현합니다.</summary>
public sealed class EquipmentChangeReceiptRepository(
    GameDbContext dbContext) :
    IEquipmentChangeReceiptRepository
{
    public Task<EquipmentChangeReceipt?> FindAsync(
        Guid playerId,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        dbContext.EquipmentChangeReceipts
            .AsNoTracking()
            .SingleOrDefaultAsync(
                receipt =>
                    receipt.PlayerId == playerId &&
                    receipt.IdempotencyKey == idempotencyKey,
                cancellationToken);

    public void Add(EquipmentChangeReceipt receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        dbContext.EquipmentChangeReceipts.Add(receipt);
    }
}
