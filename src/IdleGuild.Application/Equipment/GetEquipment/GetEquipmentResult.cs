namespace IdleGuild.Application.Equipment.GetEquipment;

/// <summary>플레이어 장비 목록과 현재 장비 전투력 합계를 반환합니다.</summary>
public sealed record GetEquipmentResult(
    Guid PlayerId,
    int EquipmentPowerBonus,
    IReadOnlyList<GetEquipmentItemResult> Items);
