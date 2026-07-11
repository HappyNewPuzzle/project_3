using IdleGuild.Infrastructure.HealthChecks;

namespace IdleGuild.Api.Tests;

/// <summary>API 테스트가 PostgreSQL 가용 상태를 성공 또는 실패로 고정하게 합니다.</summary>
internal sealed class StubDatabaseReadinessProbe(
    bool canConnect) : IDatabaseReadinessProbe
{
    public Task<bool> CanConnectAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(canConnect);
}
