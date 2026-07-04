using IdleGuild.Application.Abstractions.Persistence;
using IdleGuild.Domain.GameStates;
using Microsoft.EntityFrameworkCore;

namespace IdleGuild.Infrastructure.Persistence.Repositories;

/// <summary>플레이어 상태 저장소 인터페이스를 EF Core로 구현합니다.</summary>
public sealed class PlayerGameStateRepository(
    GameDbContext dbContext) : IPlayerGameStateRepository
{
    /// <summary>새 상태를 현재 작업 단위의 변경 추적기에 추가합니다.</summary>
    public void Add(PlayerGameState gameState)
    {
        ArgumentNullException.ThrowIfNull(gameState);
        dbContext.PlayerGameStates.Add(gameState);
    }

    /// <summary>플레이어 ID로 읽기 전용 게임 상태를 조회합니다.</summary>
    public Task<PlayerGameState?> FindByIdAsync(
        Guid playerId,
        CancellationToken cancellationToken = default) =>
        dbContext.PlayerGameStates
            .AsNoTracking()
            .SingleOrDefaultAsync(
                state => state.PlayerId == playerId,
                cancellationToken);
}
