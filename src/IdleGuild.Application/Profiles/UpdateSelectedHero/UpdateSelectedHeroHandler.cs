using IdleGuild.Application.Abstractions.Persistence;

namespace IdleGuild.Application.Profiles.UpdateSelectedHero;

/// <summary>선택 영웅을 저장하고 동시 상태 변경 충돌 시 최신 행으로 재시도합니다.</summary>
public sealed class UpdateSelectedHeroHandler(
    IPlayerGameStateRepository repository,
    IGameUnitOfWork unitOfWork)
{
    private const int MaxSaveAttempts = 3;

    public async Task<string?> HandleAsync(
        Guid playerId,
        string selectedHeroId,
        CancellationToken cancellationToken = default)
    {
        if (playerId == Guid.Empty)
        {
            throw new ArgumentException("Player ID must not be empty.", nameof(playerId));
        }

        for (var attempt = 1; attempt <= MaxSaveAttempts; attempt++)
        {
            var gameState = await repository.FindForUpdateAsync(playerId, cancellationToken);
            if (gameState is null) return null;

            gameState.SelectHero(selectedHeroId);
            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return gameState.SelectedHeroId;
            }
            catch (PersistenceConflictException) when (attempt < MaxSaveAttempts)
            {
                // 최신 선택값과 xmin을 다시 읽도록 실패한 변경 추적 상태를 제거합니다.
                unitOfWork.DiscardChanges();
            }
        }

        throw new InvalidOperationException("Selected hero could not be saved after retries.");
    }
}
