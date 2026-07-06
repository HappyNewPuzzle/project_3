using IdleGuild.Application.Abstractions.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace IdleGuild.Api.Tests;

/// <summary>실제 네트워크 없이 개발 환경의 API 전체 파이프라인을 실행합니다.</summary>
public sealed class IdleGuildApiFactory : WebApplicationFactory<Program>
{
    // OpenAPI와 Swagger UI까지 통합 테스트할 수 있도록 개발 환경을 고정합니다.
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureLogging(logging =>
            logging.ClearProviders());
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IPlayerGameStateRepository>();
            services.RemoveAll<IIdleRewardClaimRepository>();
            services.RemoveAll<IHeroUpgradeReceiptRepository>();
            services.RemoveAll<IGameUnitOfWork>();
            services.AddSingleton<InMemoryPlayerGameStateStore>();
            services.AddSingleton<IPlayerGameStateRepository>(
                provider => provider.GetRequiredService<
                    InMemoryPlayerGameStateStore>());
            services.AddSingleton<IIdleRewardClaimRepository>(
                provider => provider.GetRequiredService<
                    InMemoryPlayerGameStateStore>());
            services.AddSingleton<IHeroUpgradeReceiptRepository>(
                provider => provider.GetRequiredService<
                    InMemoryPlayerGameStateStore>());
            services.AddSingleton<IGameUnitOfWork>(
                provider => provider.GetRequiredService<
                    InMemoryPlayerGameStateStore>());
        });
    }
}
