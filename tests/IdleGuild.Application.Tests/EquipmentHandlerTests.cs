using IdleGuild.Application.Abstractions.Persistence;
using IdleGuild.Application.Equipment.ChangeEquipment;
using IdleGuild.Application.Equipment.GetEquipment;
using IdleGuild.Domain.Equipment;
using IdleGuild.Domain.GameStates;

namespace IdleGuild.Application.Tests;

/// <summary>장비 조회, 슬롯 교체와 멱등 재생 유스케이스를 검증합니다.</summary>
public sealed class EquipmentHandlerTests
{
    // 장착한 훈련용 검은 서버 카탈로그의 +1 전투력으로 조회되어야 합니다.
    [Fact]
    public async Task GetEquipment_ReturnsMasterDataAndPower()
    {
        var playerId = Guid.NewGuid();
        var repository = CreateStarterRepository(playerId);
        var handler = new GetEquipmentHandler(
            repository,
            repository);

        var result = await handler.HandleAsync(playerId);

        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(1, result.EquipmentPowerBonus);
        Assert.Single(result.Items, item => item.IsEquipped);
    }

    // 청동 검 장착은 기존 훈련용 검을 해제하고 같은 키 재요청에서 최초 결과를 재생해야 합니다.
    [Fact]
    public async Task ChangeEquipment_ReplacesSlotAndReplays()
    {
        var playerId = Guid.NewGuid();
        var repository = CreateStarterRepository(playerId);
        var bronze = (await repository.ListAsync(playerId))
            .Single(item => item.DefinitionId ==
                EquipmentCatalog.BronzeSwordId);
        var handler = new ChangeEquipmentHandler(
            repository,
            repository,
            repository,
            new StubTimeProvider(Utc(1)));

        var first = await handler.HandleAsync(
            playerId,
            bronze.EquipmentId,
            desiredEquipped: true,
            "equip-bronze");
        var replay = await handler.HandleAsync(
            playerId,
            bronze.EquipmentId,
            desiredEquipped: true,
            "equip-bronze");
        var equipped = await repository.ListEquippedAsync(
            playerId);

        Assert.NotNull(first);
        Assert.NotNull(replay);
        Assert.Equal(EquipmentChangeOutcome.Succeeded, first.Outcome);
        Assert.NotNull(first.ReplacedEquipmentId);
        Assert.True(replay.IsReplay);
        var onlyEquipped = Assert.Single(equipped);
        Assert.Equal(bronze.EquipmentId, onlyEquipped.EquipmentId);
        Assert.Equal(2, repository.SaveCount);
    }

    private static InMemoryPlayerGameStateRepository
        CreateStarterRepository(Guid playerId)
    {
        var repository =
            new InMemoryPlayerGameStateRepository();
        repository.Add(PlayerGameState.Create(
            playerId,
            Utc(0)));

        foreach (var definition in
                 EquipmentCatalog.GetStarterDefinitions())
        {
            repository.Add(PlayerEquipment.Create(
                playerId,
                definition,
                definition.DefinitionId ==
                EquipmentCatalog.TrainingSwordId,
                Utc(0)));
        }

        return repository;
    }

    private static DateTimeOffset Utc(int hour) =>
        new(2026, 7, 11, hour, 0, 0, TimeSpan.Zero);
}
