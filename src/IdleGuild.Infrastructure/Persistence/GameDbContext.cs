using IdleGuild.Domain.GameStates;
using Microsoft.EntityFrameworkCore;

namespace IdleGuild.Infrastructure.Persistence;

/// <summary>게임 도메인 객체와 PostgreSQL 테이블 사이의 작업 단위를 제공합니다.</summary>
public sealed class GameDbContext(
    DbContextOptions<GameDbContext> options) : DbContext(options)
{
    public DbSet<PlayerGameState> PlayerGameStates =>
        Set<PlayerGameState>();

    // 같은 어셈블리의 모든 IEntityTypeConfiguration 구현을 자동 적용합니다.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(GameDbContext).Assembly);
    }
}
