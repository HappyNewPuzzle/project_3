namespace IdleGuild.Application.Equipment.GetEquipment;

/// <summary>클라이언트에 표시할 보유 장비 한 건과 서버 마스터 정보를 표현합니다.</summary>
public sealed record GetEquipmentItemResult(
    Guid EquipmentId,
    string DefinitionId,
    string Name,
    string Slot,
    int PowerBonus,
    bool IsEquipped,
    DateTimeOffset AcquiredAtUtc);
