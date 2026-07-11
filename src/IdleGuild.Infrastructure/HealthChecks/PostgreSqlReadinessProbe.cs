using IdleGuild.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IdleGuild.Infrastructure.HealthChecks;

/// <summary>EF Core를 통해 PostgreSQL 연결을 실제로 열 수 있는지 확인합니다.</summary>
public sealed class PostgreSqlReadinessProbe(
    GameDbContext dbContext) : IDatabaseReadinessProbe
{
    public Task<bool> CanConnectAsync(
        CancellationToken cancellationToken = default) =>
        dbContext.Database.CanConnectAsync(
            cancellationToken);
}
