using IdleGuild.Application.Profiles.UpdateSelectedHero;
using IdleGuild.Application.Rewards.ClaimIdleReward;
using IdleGuild.Application.Rewards.PreviewIdleReward;
using IdleGuild.Domain.GameStates;
using IdleGuild.Domain.Rewards;

namespace IdleGuild.Application.Tests;

/// <summary>선택 저장과 읽기 전용 보상 미리보기 유스케이스를 검증합니다.</summary>
public sealed class SelectedHeroAndPreviewHandlerTests
{
    [Theory]
    [InlineData("black_cat")]
    [InlineData("classic")]
    public async Task UpdateSelectedHero_SavesSupportedValue(string heroId)
    {
        var repository = CreateRepository(out var playerId, Utc(0));
        var result = await new UpdateSelectedHeroHandler(repository, repository)
            .HandleAsync(playerId, heroId);

        Assert.Equal(heroId, result);
        Assert.Equal(heroId, (await repository.FindByIdAsync(playerId))!.SelectedHeroId);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task Preview_RepeatedCallsDoNotSaveOrClaim()
    {
        var repository = CreateRepository(out var playerId, Utc(0));
        var handler = new PreviewIdleRewardHandler(repository, new StubTimeProvider(Utc(3_600)));

        var first = await handler.HandleAsync(playerId);
        var second = await handler.HandleAsync(playerId);

        Assert.Equal(3_600, first!.ElapsedSeconds);
        Assert.Equal(3_600, first.ClaimableGold);
        Assert.Equal(first, second);
        Assert.Equal(0, repository.SaveCount);
        var state = await repository.FindByIdAsync(playerId);
        Assert.Equal(0, state!.Gold);
        Assert.Equal(Utc(0), state.LastIdleRewardClaimedAtUtc);
    }

    [Fact]
    public async Task PreviewThenClaim_AtSameTimeAwardsSameGold()
    {
        var repository = CreateRepository(out var playerId, Utc(0));
        var time = new StubTimeProvider(Utc(3_600));
        var preview = await new PreviewIdleRewardHandler(repository, time).HandleAsync(playerId);
        var claim = await new ClaimIdleRewardHandler(repository, repository, repository, repository, time)
            .HandleAsync(playerId, "preview-claim");

        Assert.Equal(preview!.ClaimableGold, claim!.GoldAwarded);
        Assert.Equal(3_600, claim.GoldBalanceAfter);
    }

    [Fact]
    public async Task Preview_CapsAtMaximumAccumulation()
    {
        var repository = CreateRepository(out var playerId, Utc(0));
        var result = await new PreviewIdleRewardHandler(
            repository,
            new StubTimeProvider(Utc(IdleRewardPolicy.MaxAccumulationSeconds * 2)))
            .HandleAsync(playerId);

        Assert.Equal(IdleRewardPolicy.MaxAccumulationSeconds, result!.ElapsedSeconds);
    }

    private static InMemoryPlayerGameStateRepository CreateRepository(
        out Guid playerId,
        DateTimeOffset createdAt)
    {
        playerId = Guid.NewGuid();
        var repository = new InMemoryPlayerGameStateRepository();
        repository.Add(PlayerGameState.Create(playerId, createdAt));
        return repository;
    }

    private static DateTimeOffset Utc(int seconds) =>
        new DateTimeOffset(2026, 7, 17, 0, 0, 0, TimeSpan.Zero)
            .AddSeconds(seconds);
}
