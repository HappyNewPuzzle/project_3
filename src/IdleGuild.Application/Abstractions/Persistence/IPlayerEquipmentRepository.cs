using IdleGuild.Domain.Equipment;

namespace IdleGuild.Application.Abstractions.Persistence;

/// <summary>플레이어 보유 장비의 저장, 조회와 장착 슬롯 탐색을 정의합니다.</summary>
public interface IPlayerEquipmentRepository
{
    void Add(PlayerEquipment equipment);

    Task<IReadOnlyList<PlayerEquipment>> ListAsync(
        Guid playerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlayerEquipment>> ListEquippedAsync(
        Guid playerId,
        CancellationToken cancellationToken = default);

    Task<PlayerEquipment?> FindForUpdateAsync(
        Guid playerId,
        Guid equipmentId,
        CancellationToken cancellationToken = default);

    Task<PlayerEquipment?> FindEquippedForUpdateAsync(
        Guid playerId,
        EquipmentSlot slot,
        CancellationToken cancellationToken = default);
}
