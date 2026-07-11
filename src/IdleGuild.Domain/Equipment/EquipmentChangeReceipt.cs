using IdleGuild.Domain.Requests;

namespace IdleGuild.Domain.Equipment;

/// <summary>장비 장착·해제 요청의 최초 결과를 재현하는 멱등 영수증입니다.</summary>
public sealed class EquipmentChangeReceipt
{
    // EF Core가 PostgreSQL 행을 복원할 때 사용하는 전용 생성자입니다.
    private EquipmentChangeReceipt()
    {
        IdempotencyKey = string.Empty;
    }

    private EquipmentChangeReceipt(
        Guid playerId,
        string idempotencyKey,
        Guid equipmentId,
        bool desiredEquipped,
        EquipmentChangeOutcome outcome,
        Guid? replacedEquipmentId,
        DateTimeOffset processedAtUtc)
    {
        PlayerId = playerId;
        IdempotencyKey = idempotencyKey;
        EquipmentId = equipmentId;
        DesiredEquipped = desiredEquipped;
        Outcome = outcome;
        ReplacedEquipmentId = replacedEquipmentId;
        ProcessedAtUtc = processedAtUtc;
    }

    public Guid PlayerId { get; private set; }
    public string IdempotencyKey { get; private set; }
    public Guid EquipmentId { get; private set; }
    public bool DesiredEquipped { get; private set; }
    public EquipmentChangeOutcome Outcome { get; private set; }
    public Guid? ReplacedEquipmentId { get; private set; }
    public DateTimeOffset ProcessedAtUtc { get; private set; }

    /// <summary>검증된 장비 변경 결과를 같은 요청에서 재사용할 영수증으로 만듭니다.</summary>
    public static EquipmentChangeReceipt Create(
        Guid playerId,
        string idempotencyKey,
        Guid equipmentId,
        bool desiredEquipped,
        EquipmentChangeOutcome outcome,
        Guid? replacedEquipmentId,
        DateTimeOffset processedAtUtc)
    {
        if (playerId == Guid.Empty ||
            equipmentId == Guid.Empty)
        {
            throw new ArgumentException(
                "Player and equipment IDs must not be empty.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(
            idempotencyKey);

        if (idempotencyKey.Length >
            IdempotencyPolicy.MaxKeyLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(idempotencyKey));
        }

        if (!Enum.IsDefined(outcome) ||
            processedAtUtc == default ||
            (!desiredEquipped &&
             replacedEquipmentId is not null))
        {
            throw new ArgumentException(
                "Equipment change result is invalid.");
        }

        return new EquipmentChangeReceipt(
            playerId,
            idempotencyKey.Trim(),
            equipmentId,
            desiredEquipped,
            outcome,
            replacedEquipmentId,
            processedAtUtc.ToUniversalTime());
    }
}
