namespace IdleGuild.Api.Contracts;

/// <summary>보유 장비 목록과 현재 장착 전투력 보너스를 반환합니다.</summary>
public sealed record EquipmentInventoryResponse(
    Guid PlayerId,
    int EquipmentPowerBonus,
    IReadOnlyList<EquipmentItemResponse> Items);
