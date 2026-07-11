namespace IdleGuild.Domain.Equipment;

/// <summary>서버가 신뢰하는 장비 이름, 슬롯과 전투력 보너스를 관리합니다.</summary>
public static class EquipmentCatalog
{
    public const string TrainingSwordId = "training-sword";
    public const string BronzeSwordId = "bronze-sword";
    public const int MaxDefinitionIdLength = 64;

    private static readonly IReadOnlyDictionary<
        string,
        EquipmentDefinition> Definitions =
        new Dictionary<string, EquipmentDefinition>(
            StringComparer.Ordinal)
        {
            [TrainingSwordId] = new(
                TrainingSwordId,
                "Training Sword",
                EquipmentSlot.Weapon,
                PowerBonus: 1),
            [BronzeSwordId] = new(
                BronzeSwordId,
                "Bronze Sword",
                EquipmentSlot.Weapon,
                PowerBonus: 4)
        };

    public static EquipmentDefinition GetRequired(
        string definitionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            definitionId);

        return Definitions.TryGetValue(
            definitionId,
            out var definition)
            ? definition
            : throw new ArgumentOutOfRangeException(
                nameof(definitionId),
                "Equipment definition is not registered.");
    }

    /// <summary>신규 플레이어에게 지급할 두 무기 마스터를 순서대로 반환합니다.</summary>
    public static IReadOnlyList<EquipmentDefinition>
        GetStarterDefinitions() =>
        [
            GetRequired(TrainingSwordId),
            GetRequired(BronzeSwordId)
        ];

    /// <summary>서버가 확인한 장착 인스턴스의 마스터 전투력 합계를 계산합니다.</summary>
    public static int CalculateEquippedPowerBonus(
        IEnumerable<PlayerEquipment> equipment) =>
        equipment
            .Where(item => item.IsEquipped)
            .Sum(item => GetRequired(
                item.DefinitionId).PowerBonus);
}
