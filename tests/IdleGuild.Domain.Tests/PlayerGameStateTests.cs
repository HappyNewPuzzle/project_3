using IdleGuild.Domain.GameStates;

namespace IdleGuild.Domain.Tests;

/// <summary>새 플레이어 게임 상태의 기본값과 생성 불변식을 검증합니다.</summary>
public sealed class PlayerGameStateTests
{
    // 새 계정은 레벨·스테이지 1과 골드 0에서 시작해야 합니다.
    [Fact]
    public void Create_InitializesNewPlayerDefaults()
    {
        var playerId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(
            2026, 6, 30, 10, 0, 0, TimeSpan.FromHours(9));

        var state = PlayerGameState.Create(
            playerId,
            createdAt);

        Assert.Equal(playerId, state.PlayerId);
        Assert.Equal(0, state.Gold);
        Assert.Equal(1, state.HeroLevel);
        Assert.Equal(1, state.HighestStage);
        Assert.Equal(TimeSpan.Zero, state.CreatedAtUtc.Offset);
        Assert.Equal(state.CreatedAtUtc, state.LastIdleRewardClaimedAtUtc);
    }

    // 빈 식별자로 생성된 상태가 DB에 유입되지 않도록 도메인 경계에서 거부합니다.
    [Fact]
    public void Create_WithEmptyPlayerId_Throws()
    {
        var action = () => PlayerGameState.Create(
            Guid.Empty,
            DateTimeOffset.UtcNow);

        Assert.Throws<ArgumentException>(action);
    }
}
