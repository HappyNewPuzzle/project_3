using IdleGuild.Application.Abstractions.Persistence;
using IdleGuild.Domain.Equipment;

namespace IdleGuild.Application.Equipment.GetEquipment;

/// <summary>플레이어 보유 장비를 서버 마스터 데이터와 결합해 조회합니다.</summary>
public sealed class GetEquipmentHandler(
    IPlayerGameStateRepository gameStateRepository,
    IPlayerEquipmentRepository equipmentRepository)
{
    public async Task<GetEquipmentResult?> HandleAsync(
        Guid playerId,
        CancellationToken cancellationToken = default)
    {
        var state = await gameStateRepository.FindByIdAsync(
            playerId,
            cancellationToken);

        if (state is null)
        {
            return null;
        }

        var equipment = await equipmentRepository.ListAsync(
            playerId,
            cancellationToken);
        var items = equipment
            .Select(item =>
            {
                var definition = EquipmentCatalog.GetRequired(
                    item.DefinitionId);
                return new GetEquipmentItemResult(
                    item.EquipmentId,
                    definition.DefinitionId,
                    definition.Name,
                    definition.Slot.ToString(),
                    definition.PowerBonus,
                    item.IsEquipped,
                    item.AcquiredAtUtc);
            })
            .OrderBy(item => item.Slot)
            .ThenBy(item => item.DefinitionId)
            .ToArray();

        return new GetEquipmentResult(
            playerId,
            items.Where(item => item.IsEquipped)
                .Sum(item => item.PowerBonus),
            items);
    }
}
