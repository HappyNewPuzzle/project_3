namespace IdleGuild.Infrastructure.HealthChecks;

/// <summary>API가 요청을 처리하기 전에 게임 DB 연결 가능 여부를 확인합니다.</summary>
public interface IDatabaseReadinessProbe
{
    Task<bool> CanConnectAsync(
        CancellationToken cancellationToken = default);
}
