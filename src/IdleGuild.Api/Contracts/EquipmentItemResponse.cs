namespace IdleGuild.Api.Contracts;

/// <summary>클라이언트에 표시할 플레이어 보유 장비 한 건입니다.</summary>
public sealed record EquipmentItemResponse(
    Guid EquipmentId,
    string DefinitionId,
    string Name,
    string Slot,
    int PowerBonus,
    bool IsEquipped,
    DateTimeOffset AcquiredAtUtc);
