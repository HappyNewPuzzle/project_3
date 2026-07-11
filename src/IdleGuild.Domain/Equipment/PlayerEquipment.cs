namespace IdleGuild.Domain.Equipment;

/// <summary>플레이어가 소유한 장비 인스턴스와 현재 장착 여부를 표현합니다.</summary>
public sealed class PlayerEquipment
{
    // EF Core가 PostgreSQL 행을 복원할 때 사용하는 전용 생성자입니다.
    private PlayerEquipment()
    {
        DefinitionId = string.Empty;
    }

    private PlayerEquipment(
        Guid equipmentId,
        Guid playerId,
        EquipmentDefinition definition,
        bool isEquipped,
        DateTimeOffset acquiredAtUtc)
    {
        EquipmentId = equipmentId;
        PlayerId = playerId;
        DefinitionId = definition.DefinitionId;
        Slot = definition.Slot;
        IsEquipped = isEquipped;
        AcquiredAtUtc = acquiredAtUtc;
    }

    public Guid EquipmentId { get; private set; }

    public Guid PlayerId { get; private set; }

    public string DefinitionId { get; private set; }

    public EquipmentSlot Slot { get; private set; }

    public bool IsEquipped { get; private set; }

    public DateTimeOffset AcquiredAtUtc { get; private set; }

    public uint Version { get; private set; }

    /// <summary>서버 마스터 정의로만 플레이어 보유 장비를 생성합니다.</summary>
    public static PlayerEquipment Create(
        Guid playerId,
        EquipmentDefinition definition,
        bool isEquipped,
        DateTimeOffset acquiredAtUtc)
    {
        if (playerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Player ID must not be empty.",
                nameof(playerId));
        }

        ArgumentNullException.ThrowIfNull(definition);

        if (acquiredAtUtc == default)
        {
            throw new ArgumentException(
                "Acquisition time must be provided.",
                nameof(acquiredAtUtc));
        }

        return new PlayerEquipment(
            Guid.NewGuid(),
            playerId,
            definition,
            isEquipped,
            acquiredAtUtc.ToUniversalTime());
    }

    /// <summary>Application이 슬롯 불변식을 조정한 뒤 이 인스턴스의 장착 상태를 바꿉니다.</summary>
    public bool SetEquipped(bool desiredEquipped)
    {
        if (IsEquipped == desiredEquipped)
        {
            return false;
        }

        IsEquipped = desiredEquipped;
        return true;
    }
}
