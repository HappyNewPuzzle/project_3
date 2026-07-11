using IdleGuild.Infrastructure.HealthChecks;

namespace IdleGuild.Infrastructure.Tests;

/// <summary>readiness probe가 실제 PostgreSQL 연결을 열 수 있는지 검증합니다.</summary>
[Collection(PostgreSqlTestCollection.Name)]
public sealed class PostgreSqlReadinessProbeTests(
    PostgreSqlDatabaseFixture database)
{
    // Migration이 적용된 테스트 PostgreSQL은 트래픽 처리 준비 상태여야 합니다.
    [Fact]
    public async Task CanConnectAsync_WithRunningPostgreSql_ReturnsTrue()
    {
        await using var context =
            database.CreateDbContext();
        var probe = new PostgreSqlReadinessProbe(context);

        var canConnect = await probe.CanConnectAsync();

        Assert.True(canConnect);
    }
}
