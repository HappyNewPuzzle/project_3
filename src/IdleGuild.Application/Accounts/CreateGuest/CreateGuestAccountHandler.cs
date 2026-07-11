using IdleGuild.Application.Abstractions.Authentication;
using IdleGuild.Application.Abstractions.Persistence;
using IdleGuild.Domain.GameStates;
using IdleGuild.Domain.Equipment;

namespace IdleGuild.Application.Accounts.CreateGuest;

/// <summary>새 게스트 상태를 생성·저장하고 액세스 토큰을 발급합니다.</summary>
public sealed class CreateGuestAccountHandler(
    IPlayerGameStateRepository repository,
    IPlayerEquipmentRepository equipmentRepository,
    IGameUnitOfWork unitOfWork,
    IAccessTokenIssuer accessTokenIssuer,
    TimeProvider timeProvider)
{
    /// <summary>서버가 생성한 ID와 시각으로 독립된 게스트 계정을 만듭니다.</summary>
    public async Task<CreateGuestAccountResult> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var playerId = Guid.NewGuid();
        var createdAtUtc = timeProvider.GetUtcNow();
        var gameState = PlayerGameState.Create(
            playerId,
            createdAtUtc);
        var accessToken = accessTokenIssuer.Issue(playerId);

        repository.Add(gameState);

        foreach (var definition in
                 EquipmentCatalog.GetStarterDefinitions())
        {
            // 훈련용 검만 초기 장착하고 청동 검은 교체 학습용으로 보유시킵니다.
            equipmentRepository.Add(
                PlayerEquipment.Create(
                    playerId,
                    definition,
                    isEquipped: definition.DefinitionId ==
                        EquipmentCatalog.TrainingSwordId,
                    createdAtUtc));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateGuestAccountResult(
            playerId,
            accessToken.Value,
            accessToken.ExpiresAtUtc);
    }
}
