using IdleGuild.Application.Accounts.CreateGuest;

namespace IdleGuild.Application.Tests;

/// <summary>게스트 생성 유스케이스의 상태 저장과 토큰 결과를 검증합니다.</summary>
public sealed class CreateGuestAccountHandlerTests
{
    // 하나의 유스케이스가 초기 상태 저장과 같은 플레이어의 토큰 발급을 완료해야 합니다.
    [Fact]
    public async Task HandleAsync_CreatesStateAndReturnsToken()
    {
        var now = new DateTimeOffset(
            2026, 7, 4, 1, 2, 3, TimeSpan.Zero);
        var expiresAt = now.AddDays(30);
        var repository =
            new InMemoryPlayerGameStateRepository();
        var tokenIssuer = new StubAccessTokenIssuer(
            "test-access-token",
            expiresAt);
        var handler = new CreateGuestAccountHandler(
            repository,
            repository,
            tokenIssuer,
            new StubTimeProvider(now));

        var result = await handler.HandleAsync();
        var saved = await repository.FindByIdAsync(
            result.PlayerId);

        Assert.NotEqual(Guid.Empty, result.PlayerId);
        Assert.Equal("test-access-token", result.AccessToken);
        Assert.Equal(expiresAt, result.ExpiresAtUtc);
        Assert.Equal(result.PlayerId, tokenIssuer.IssuedPlayerId);
        Assert.Equal(1, repository.SaveCount);
        Assert.NotNull(saved);
        Assert.Equal(now, saved.CreatedAtUtc);
        Assert.Equal(0, saved.Gold);
    }
}
