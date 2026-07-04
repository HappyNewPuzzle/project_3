using IdleGuild.Application.Abstractions.Authentication;
using IdleGuild.Application.Abstractions.Persistence;
using IdleGuild.Domain.GameStates;

namespace IdleGuild.Application.Accounts.CreateGuest;

/// <summary>새 게스트 상태를 생성·저장하고 액세스 토큰을 발급합니다.</summary>
public sealed class CreateGuestAccountHandler(
    IPlayerGameStateRepository repository,
    IGameUnitOfWork unitOfWork,
    IAccessTokenIssuer accessTokenIssuer,
    TimeProvider timeProvider)
{
    /// <summary>서버가 생성한 ID와 시각으로 독립된 게스트 계정을 만듭니다.</summary>
    public async Task<CreateGuestAccountResult> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var playerId = Guid.NewGuid();
        var gameState = PlayerGameState.Create(
            playerId,
            timeProvider.GetUtcNow());
        var accessToken = accessTokenIssuer.Issue(playerId);

        repository.Add(gameState);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateGuestAccountResult(
            playerId,
            accessToken.Value,
            accessToken.ExpiresAtUtc);
    }
}
