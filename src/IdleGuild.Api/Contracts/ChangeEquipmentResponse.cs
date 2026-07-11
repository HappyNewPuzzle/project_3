namespace IdleGuild.Api.Contracts;

/// <summary>장비 장착 상태 변경의 최초 또는 멱등 재생 결과입니다.</summary>
public sealed record ChangeEquipmentResponse(
    string IdempotencyKey,
    Guid EquipmentId,
    bool IsEquipped,
    string Outcome,
    Guid? ReplacedEquipmentId,
    DateTimeOffset ProcessedAtUtc,
    bool IsReplay);
