using IdleGuild.Application.GameStates.GetGameState;
using IdleGuild.Domain.GameStates;

namespace IdleGuild.Application.Tests;

/// <summary>플레이어 ID 기준 게임 상태 조회 결과를 검증합니다.</summary>
public sealed class GetGameStateHandlerTests
{
    // 저장된 Domain 객체가 클라이언트 노출용 결과로 정확히 변환되어야 합니다.
    [Fact]
    public async Task HandleAsync_WithExistingPlayer_ReturnsState()
    {
        var playerId = Guid.NewGuid();
        var now = new DateTimeOffset(
            2026, 7, 4, 1, 2, 3, TimeSpan.Zero);
        var repository =
            new InMemoryPlayerGameStateRepository();
        repository.Add(PlayerGameState.Create(
            playerId,
            now));
        var handler = new GetGameStateHandler(
            repository,
            repository);

        var result = await handler.HandleAsync(playerId);

        Assert.NotNull(result);
        Assert.Equal(playerId, result.PlayerId);
        Assert.Equal(0, result.Gold);
        Assert.Equal(1, result.HeroLevel);
        Assert.Equal(10, result.HeroPower);
        Assert.Equal(0, result.EquipmentPowerBonus);
        Assert.Equal(1, result.HighestStage);
        Assert.Equal(0, result.ProductionBonusPercent);
        Assert.Equal(0, result.IdleRewardRemainderHundredths);
        Assert.Equal(now, result.LastIdleRewardClaimedAtUtc);
    }

    // 존재하지 않는 플레이어를 다른 상태로 대체하지 않고 null로 반환해야 합니다.
    [Fact]
    public async Task HandleAsync_WithMissingPlayer_ReturnsNull()
    {
        var repository =
            new InMemoryPlayerGameStateRepository();
        var handler = new GetGameStateHandler(
            repository,
            repository);

        var result = await handler.HandleAsync(
            Guid.NewGuid());

        Assert.Null(result);
    }
}
