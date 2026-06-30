using IdleGuild.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IdleGuild.Infrastructure;

/// <summary>PostgreSQL을 포함한 Infrastructure 구현을 의존성 컨테이너에 등록합니다.</summary>
public static class DependencyInjection
{
    /// <summary>게임 DbContext가 지정된 PostgreSQL 연결을 사용하도록 구성합니다.</summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<GameDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }
}
