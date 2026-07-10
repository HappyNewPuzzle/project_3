using IdleGuild.Application.Abstractions.Persistence;
using IdleGuild.Domain.Stages;

namespace IdleGuild.Application.Admin.Players.GetAdminPlayer;

/// <summary>관리자에게 특정 플레이어의 현재 서버 상태를 읽기 전용으로 제공합니다.</summary>
public sealed class GetAdminPlayerHandler(
    IPlayerGameStateRepository repository)
{
    /// <summary>플레이어 상태가 존재할 때만 운영 조회 결과로 변환합니다.</summary>
    public async Task<GetAdminPlayerResult?> HandleAsync(
        Guid playerId,
        CancellationToken cancellationToken = default)
    {
        if (playerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Player ID must not be empty.",
                nameof(playerId));
        }

        var state = await repository.FindByIdAsync(
            playerId,
            cancellationToken);

        return state is null
            ? null
            : new GetAdminPlayerResult(
                state.PlayerId,
                state.Gold,
                state.HeroLevel,
                state.HighestStage,
                StageChallengePolicy
                    .CalculateProductionBonusPercent(
                        state.HighestStage),
                state.IdleRewardRemainderHundredths,
                state.CreatedAtUtc,
                state.LastIdleRewardClaimedAtUtc,
                state.Version);
    }
}
