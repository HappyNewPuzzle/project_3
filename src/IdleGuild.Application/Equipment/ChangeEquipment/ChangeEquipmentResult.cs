using IdleGuild.Domain.Equipment;

namespace IdleGuild.Application.Equipment.ChangeEquipment;

/// <summary>장착 상태 변경의 최초 또는 재생 결과를 반환합니다.</summary>
public sealed record ChangeEquipmentResult(
    string IdempotencyKey,
    Guid EquipmentId,
    bool IsEquipped,
    EquipmentChangeOutcome Outcome,
    Guid? ReplacedEquipmentId,
    DateTimeOffset ProcessedAtUtc,
    bool IsReplay);
