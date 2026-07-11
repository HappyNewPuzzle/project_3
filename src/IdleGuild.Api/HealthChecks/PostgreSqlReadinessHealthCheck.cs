using IdleGuild.Infrastructure.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace IdleGuild.Api.HealthChecks;

/// <summary>PostgreSQL 연결 결과를 배포 플랫폼이 이해하는 readiness 상태로 변환합니다.</summary>
public sealed class PostgreSqlReadinessHealthCheck(
    IDatabaseReadinessProbe databaseProbe) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await databaseProbe.CanConnectAsync(
                cancellationToken);

            return canConnect
                ? HealthCheckResult.Healthy(
                    "PostgreSQL is accepting connections.")
                : HealthCheckResult.Unhealthy(
                    "PostgreSQL is not accepting connections.");
        }
        catch (Exception exception)
        {
            // 연결 예외는 API 500으로 전파하지 않고 readiness 실패로 보고합니다.
            return HealthCheckResult.Unhealthy(
                "PostgreSQL readiness check failed.",
                exception);
        }
    }
}
