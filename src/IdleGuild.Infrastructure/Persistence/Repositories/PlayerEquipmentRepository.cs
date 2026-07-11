using IdleGuild.Application.Abstractions.Persistence;
using IdleGuild.Domain.Equipment;
using Microsoft.EntityFrameworkCore;

namespace IdleGuild.Infrastructure.Persistence.Repositories;

/// <summary>플레이어 보유 장비 조회와 장착 변경 추적을 EF Core로 구현합니다.</summary>
public sealed class PlayerEquipmentRepository(
    GameDbContext dbContext) : IPlayerEquipmentRepository
{
    public void Add(PlayerEquipment equipment)
    {
        ArgumentNullException.ThrowIfNull(equipment);
        dbContext.PlayerEquipment.Add(equipment);
    }

    public async Task<IReadOnlyList<PlayerEquipment>> ListAsync(
        Guid playerId,
        CancellationToken cancellationToken = default) =>
        await dbContext.PlayerEquipment
            .AsNoTracking()
            .Where(item => item.PlayerId == playerId)
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<PlayerEquipment>>
        ListEquippedAsync(
            Guid playerId,
            CancellationToken cancellationToken = default) =>
        await dbContext.PlayerEquipment
            .AsNoTracking()
            .Where(item =>
                item.PlayerId == playerId &&
                item.IsEquipped)
            .ToArrayAsync(cancellationToken);

    public Task<PlayerEquipment?> FindForUpdateAsync(
        Guid playerId,
        Guid equipmentId,
        CancellationToken cancellationToken = default) =>
        dbContext.PlayerEquipment.SingleOrDefaultAsync(
            item =>
                item.PlayerId == playerId &&
                item.EquipmentId == equipmentId,
            cancellationToken);

    public Task<PlayerEquipment?> FindEquippedForUpdateAsync(
        Guid playerId,
        EquipmentSlot slot,
        CancellationToken cancellationToken = default) =>
        dbContext.PlayerEquipment.SingleOrDefaultAsync(
            item =>
                item.PlayerId == playerId &&
                item.Slot == slot &&
                item.IsEquipped,
            cancellationToken);
}
