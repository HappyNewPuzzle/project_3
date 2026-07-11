using IdleGuild.Domain.Equipment;
using IdleGuild.Domain.Stages;

namespace IdleGuild.Domain.Tests;

/// <summary>장비 마스터, 보유 인스턴스와 전투력 보너스 규칙을 검증합니다.</summary>
public sealed class EquipmentTests
{
    // 클라이언트 입력이 아니라 서버 카탈로그가 장비 슬롯과 전투력을 결정해야 합니다.
    [Fact]
    public void Catalog_ReturnsServerOwnedDefinition()
    {
        var definition = EquipmentCatalog.GetRequired(
            EquipmentCatalog.BronzeSwordId);

        Assert.Equal(EquipmentSlot.Weapon, definition.Slot);
        Assert.Equal(4, definition.PowerBonus);
    }

    // 장비 장착 상태는 의미 있는 메서드로만 바뀌고 같은 상태 요청은 변경되지 않아야 합니다.
    [Fact]
    public void SetEquipped_WithSameState_ReturnsFalse()
    {
        var equipment = PlayerEquipment.Create(
            Guid.NewGuid(),
            EquipmentCatalog.GetRequired(
                EquipmentCatalog.TrainingSwordId),
            isEquipped: true,
            DateTimeOffset.UtcNow);

        var changed = equipment.SetEquipped(true);

        Assert.False(changed);
        Assert.True(equipment.IsEquipped);
    }

    // 전투력은 영웅 레벨 기본값과 서버 장비 보너스를 정확히 합산해야 합니다.
    [Fact]
    public void HeroPower_IncludesEquipmentBonus()
    {
        var power = StageChallengePolicy.CalculateHeroPower(
            heroLevel: 2,
            equipmentPowerBonus: 4);

        Assert.Equal(24, power);
    }
}
