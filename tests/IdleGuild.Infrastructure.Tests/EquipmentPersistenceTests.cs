using IdleGuild.Application.Equipment.ChangeEquipment;
using IdleGuild.Domain.Equipment;
using IdleGuild.Domain.GameStates;
using IdleGuild.Infrastructure.Persistence;
using IdleGuild.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace IdleGuild.Infrastructure.Tests;

/// <summary>장비 저장, 슬롯 유일 제약과 멱등 장착을 실제 PostgreSQL에서 검증합니다.</summary>
[Collection(PostgreSqlTestCollection.Name)]
public sealed class EquipmentPersistenceTests(
    PostgreSqlDatabaseFixture database)
{
    // 시작 장비와 xmin은 별도 DbContext에서도 그대로 복원되어야 합니다.
    [Fact]
    public async Task StarterEquipment_RoundTripsThroughPostgreSql()
    {
        var playerId = Guid.NewGuid();
        var acquiredAt = Utc(0);

        await using (var writeContext =
                     database.CreateDbContext())
        {
            writeContext.PlayerGameStates.Add(
                PlayerGameState.Create(
                    playerId,
                    acquiredAt));
            foreach (var definition in
                     EquipmentCatalog.GetStarterDefinitions())
            {
                writeContext.PlayerEquipment.Add(
                    PlayerEquipment.Create(
                        playerId,
                        definition,
                        definition.DefinitionId ==
                        EquipmentCatalog.TrainingSwordId,
                        acquiredAt));
            }

            await writeContext.SaveChangesAsync();
        }

        await using var readContext =
            database.CreateDbContext();
        var saved = await readContext.PlayerEquipment
            .AsNoTracking()
            .Where(item => item.PlayerId == playerId)
            .ToArrayAsync();

        Assert.Equal(2, saved.Length);
        Assert.Single(saved, item => item.IsEquipped);
        Assert.All(saved, item => Assert.True(item.Version > 0));
    }

    // 애플리케이션 실수로 같은 슬롯 두 개를 장착해도 DB 부분 유일 인덱스가 막아야 합니다.
    [Fact]
    public async Task TwoEquippedWeapons_ViolatesDatabaseConstraint()
    {
        var playerId = Guid.NewGuid();
        await using var context =
            database.CreateDbContext();
        context.PlayerGameStates.Add(
            PlayerGameState.Create(playerId, Utc(0)));
        context.PlayerEquipment.AddRange(
            PlayerEquipment.Create(
                playerId,
                EquipmentCatalog.GetRequired(
                    EquipmentCatalog.TrainingSwordId),
                isEquipped: true,
                Utc(0)),
            PlayerEquipment.Create(
                playerId,
                EquipmentCatalog.GetRequired(
                    EquipmentCatalog.BronzeSwordId),
                isEquipped: true,
                Utc(0)));

        var action = () => context.SaveChangesAsync();

        await Assert.ThrowsAsync<DbUpdateException>(action);
    }

    // 실제 Repository와 Unit of Work는 슬롯 교체와 같은 키 재생을 한 영수증으로 저장해야 합니다.
    [Fact]
    public async Task ChangeEquipment_PersistsReplacementAndReceiptOnce()
    {
        var playerId = Guid.NewGuid();
        Guid bronzeId;

        await using (var seedContext =
                     database.CreateDbContext())
        {
            seedContext.PlayerGameStates.Add(
                PlayerGameState.Create(playerId, Utc(0)));
            var training = PlayerEquipment.Create(
                playerId,
                EquipmentCatalog.GetRequired(
                    EquipmentCatalog.TrainingSwordId),
                isEquipped: true,
                Utc(0));
            var bronze = PlayerEquipment.Create(
                playerId,
                EquipmentCatalog.GetRequired(
                    EquipmentCatalog.BronzeSwordId),
                isEquipped: false,
                Utc(0));
            bronzeId = bronze.EquipmentId;
            seedContext.PlayerEquipment.AddRange(
                training,
                bronze);
            await seedContext.SaveChangesAsync();
        }

        await using (var commandContext =
                     database.CreateDbContext())
        {
            var handler = new ChangeEquipmentHandler(
                new PlayerEquipmentRepository(commandContext),
                new EquipmentChangeReceiptRepository(
                    commandContext),
                new EfGameUnitOfWork(commandContext),
                new FixedTimeProvider(Utc(1)));

            var first = await handler.HandleAsync(
                playerId,
                bronzeId,
                desiredEquipped: true,
                "equip-bronze-db");
            var replay = await handler.HandleAsync(
                playerId,
                bronzeId,
                desiredEquipped: true,
                "equip-bronze-db");

            Assert.NotNull(first);
            Assert.NotNull(replay);
            Assert.True(replay.IsReplay);
        }

        await using var verifyContext =
            database.CreateDbContext();
        var equipped = await verifyContext.PlayerEquipment
            .AsNoTracking()
            .Where(item =>
                item.PlayerId == playerId &&
                item.IsEquipped)
            .SingleAsync();
        var receiptCount = await verifyContext
            .EquipmentChangeReceipts
            .CountAsync(receipt =>
                receipt.PlayerId == playerId);

        Assert.Equal(bronzeId, equipped.EquipmentId);
        Assert.Equal(1, receiptCount);
    }

    private static DateTimeOffset Utc(int hour) =>
        new(2026, 7, 11, hour, 0, 0, TimeSpan.Zero);

    private sealed class FixedTimeProvider(
        DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
