using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace IdleGuild.Infrastructure.Persistence;

/// <summary>EF CLI가 API를 실행하지 않고 Migration 모델을 생성하게 합니다.</summary>
public sealed class GameDbContextFactory :
    IDesignTimeDbContextFactory<GameDbContext>
{
    /// <summary>설계 시점 전용 연결 설정으로 GameDbContext를 생성합니다.</summary>
    public GameDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(
            "ConnectionStrings__GameDatabase")
            ?? "Host=localhost;Port=5432;Database=idleguild;Username=idleguild;Password=CHANGE_ME";

        var options = new DbContextOptionsBuilder<GameDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new GameDbContext(options);
    }
}
