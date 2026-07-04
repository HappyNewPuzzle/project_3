using IdleGuild.Application.Abstractions.Authentication;
using IdleGuild.Application.Abstractions.Persistence;
using IdleGuild.Infrastructure.Authentication;
using IdleGuild.Infrastructure.Persistence;
using IdleGuild.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IdleGuild.Infrastructure;

/// <summary>PostgreSQL을 포함한 Infrastructure 구현을 의존성 컨테이너에 등록합니다.</summary>
public static class DependencyInjection
{
    /// <summary>게임 DbContext가 지정된 PostgreSQL 연결을 사용하도록 구성합니다.</summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        JwtOptions jwtOptions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentNullException.ThrowIfNull(jwtOptions);
        jwtOptions.Validate();

        services.AddDbContext<GameDbContext>(options =>
            options.UseNpgsql(connectionString));
        services.AddScoped<IPlayerGameStateRepository,
            PlayerGameStateRepository>();
        services.AddScoped<IGameUnitOfWork>(provider =>
            provider.GetRequiredService<GameDbContext>());
        services.AddSingleton(jwtOptions);
        services.AddSingleton<IAccessTokenIssuer,
            JwtAccessTokenIssuer>();

        return services;
    }
}
