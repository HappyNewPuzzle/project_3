using IdleGuild.Domain.Equipment;

namespace IdleGuild.Application.Abstractions.Persistence;

/// <summary>장비 장착 상태 변경 영수증의 조회와 추가를 정의합니다.</summary>
public interface IEquipmentChangeReceiptRepository
{
    Task<EquipmentChangeReceipt?> FindAsync(
        Guid playerId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    void Add(EquipmentChangeReceipt receipt);
}
