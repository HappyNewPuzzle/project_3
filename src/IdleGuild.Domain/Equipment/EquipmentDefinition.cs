namespace IdleGuild.Domain.Equipment;

/// <summary>모든 플레이어가 공유하는 장비 마스터 데이터 한 건입니다.</summary>
public sealed record EquipmentDefinition(
    string DefinitionId,
    string Name,
    EquipmentSlot Slot,
    int PowerBonus);
