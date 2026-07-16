using IdleGuild.Application.Abstractions.Persistence;

namespace IdleGuild.Application.GameStates.SyncProgression;

public sealed class SyncProgressionHandler(
    IPlayerGameStateRepository repository,
    IGameUnitOfWork unitOfWork)
{
    public async Task<bool> HandleAsync(
        Guid playerId,
        int attackLevel,
        int attackSpeedLevel,
        int criticalLevel,
        int prestigeLevel,
        int soulStones,
        int equipmentTier,
        int equipmentCount,
        int unlockedRegion,
        int skillOneLevel,
        int skillTwoLevel,
        int skillThreeLevel,
        CancellationToken cancellationToken = default)
    {
        var state = await repository.FindForUpdateAsync(playerId, cancellationToken);
        if (state is null) return false;
        state.SynchronizeProgression(attackLevel, attackSpeedLevel, criticalLevel, prestigeLevel, soulStones,
            equipmentTier, equipmentCount, unlockedRegion, skillOneLevel, skillTwoLevel, skillThreeLevel);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
