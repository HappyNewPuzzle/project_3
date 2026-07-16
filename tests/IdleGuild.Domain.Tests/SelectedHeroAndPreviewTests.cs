using IdleGuild.Domain.GameStates;
using IdleGuild.Domain.Rewards;

namespace IdleGuild.Domain.Tests;

/// <summary>선택 영웅 불변식과 비변경 방치 보상 미리보기를 검증합니다.</summary>
public sealed class SelectedHeroAndPreviewTests
{
    [Fact]
    public void NewPlayer_DefaultsToGirl()
    {
        var state = PlayerGameState.Create(Guid.NewGuid(), Utc(0));
        Assert.Equal(SelectedHeroPolicy.DefaultHeroId, state.SelectedHeroId);
    }

    [Theory]
    [InlineData("black_cat")]
    [InlineData("classic")]
    public void SelectHero_SupportedIdIsStored(string heroId)
    {
        var state = PlayerGameState.Create(Guid.NewGuid(), Utc(0));
        state.SelectHero(heroId);
        Assert.Equal(heroId, state.SelectedHeroId);
    }

    [Fact]
    public void SelectHero_UnsupportedIdThrows()
    {
        var state = PlayerGameState.Create(Guid.NewGuid(), Utc(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => state.SelectHero("hacker"));
    }

    [Fact]
    public void Preview_UsesElapsedTimeWithoutChangingState()
    {
        var state = PlayerGameState.Create(Guid.NewGuid(), Utc(0));
        var preview = state.PreviewIdleReward(Utc(3_600));

        Assert.Equal(3_600, preview.ElapsedSeconds);
        Assert.Equal(3_600, preview.ClaimableGold);
        Assert.Equal(IdleRewardPolicy.MaxAccumulationSeconds, preview.MaximumAccumulationSeconds);
        Assert.Equal(0, state.Gold);
        Assert.Equal(Utc(0), state.LastIdleRewardClaimedAtUtc);
        Assert.Equal(0, state.IdleRewardRemainderHundredths);
    }

    [Fact]
    public void Preview_CapsAtMaximumAccumulation()
    {
        var state = PlayerGameState.Create(Guid.NewGuid(), Utc(0));
        var preview = state.PreviewIdleReward(Utc(IdleRewardPolicy.MaxAccumulationSeconds * 2));
        Assert.Equal(IdleRewardPolicy.MaxAccumulationSeconds, preview.ElapsedSeconds);
        Assert.Equal(IdleRewardPolicy.MaxAccumulationSeconds, preview.ClaimableGold);
    }

    private static DateTimeOffset Utc(int seconds) =>
        new DateTimeOffset(2026, 7, 17, 0, 0, 0, TimeSpan.Zero)
            .AddSeconds(seconds);
}
