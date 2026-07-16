using IdleGuild.Application.Abstractions.Persistence;
using IdleGuild.Domain.Stages;
using IdleGuild.Domain.Equipment;

namespace IdleGuild.Application.GameStates.GetGameState;

/// <summary>인증된 플레이어 ID에 해당하는 게임 상태만 조회합니다.</summary>
public sealed class GetGameStateHandler(
    IPlayerGameStateRepository repository,
    IPlayerEquipmentRepository equipmentRepository)
{
    /// <summary>저장된 상태를 외부 노출용 Application 결과로 변환합니다.</summary>
    public async Task<GetGameStateResult?> HandleAsync(
        Guid playerId,
        CancellationToken cancellationToken = default)
    {
        var gameState = await repository.FindByIdAsync(
            playerId,
            cancellationToken);

        if (gameState is null)
        {
            return null;
        }

        var equipped = await equipmentRepository
            .ListEquippedAsync(
                playerId,
                cancellationToken);
        var equipmentPowerBonus = EquipmentCatalog
            .CalculateEquippedPowerBonus(equipped);

        return new GetGameStateResult(
                gameState.PlayerId,
                gameState.Gold,
                gameState.HeroLevel,
                StageChallengePolicy.CalculateHeroPower(
                    gameState.HeroLevel,
                    equipmentPowerBonus),
                equipmentPowerBonus,
                gameState.HighestStage,
                gameState.AttackLevel,
                gameState.AttackSpeedLevel,
                gameState.CriticalLevel,
                gameState.PrestigeLevel,
                gameState.SoulStones,
                gameState.EquipmentTier,
                gameState.EquipmentCount,
                gameState.UnlockedRegion,
                gameState.SkillOneLevel,
                gameState.SkillTwoLevel,
                gameState.SkillThreeLevel,
                StageChallengePolicy
                    .CalculateProductionBonusPercent(
                        gameState.HighestStage),
                gameState.IdleRewardRemainderHundredths,
                gameState.LastIdleRewardClaimedAtUtc);
    }
}
