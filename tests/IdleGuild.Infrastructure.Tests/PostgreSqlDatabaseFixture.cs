using IdleGuild.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace IdleGuild.Infrastructure.Tests;

/// <summary>테스트마다 공유할 일회용 PostgreSQL과 최신 Migration 스키마를 준비합니다.</summary>
public sealed class PostgreSqlDatabaseFixture : IAsyncLifetime
{
    private const string ExternalConnectionVariable =
        "IDLEGUILD_TEST_POSTGRES_CONNECTION_STRING";

    private readonly PostgreSqlContainer? _container;
    private readonly string? _externalConnectionString;

    public PostgreSqlDatabaseFixture()
    {
        _externalConnectionString = Environment.GetEnvironmentVariable(
            ExternalConnectionVariable);

        // 외부 DB가 없으면 운영과 같은 주 버전의 독립 컨테이너를 자동 생성합니다.
        if (string.IsNullOrWhiteSpace(_externalConnectionString))
        {
            _container = new PostgreSqlBuilder("postgres:18-alpine")
                .WithDatabase("idleguild_tests")
                .WithUsername("idleguild")
                .WithPassword("idleguild_tests")
                .Build();
        }
    }

    public string ConnectionString
    {
        get
        {
            var source = _externalConnectionString
                ?? _container!.GetConnectionString();

            // 각 동시성 테스트가 이전 테스트의 물리 연결 상태와 독립적으로 실행되게 합니다.
            return new NpgsqlConnectionStringBuilder(source)
            {
                Pooling = false
            }.ConnectionString;
        }
    }

    /// <summary>PostgreSQL을 시작하고 모든 Migration을 순서대로 적용합니다.</summary>
    public async Task InitializeAsync()
    {
        if (_container is not null)
        {
            await _container.StartAsync();
        }

        await using var context = CreateDbContext();
        await context.Database.MigrateAsync();
    }

    /// <summary>테스트가 끝나면 컨테이너와 임시 데이터를 모두 제거합니다.</summary>
    public Task DisposeAsync() =>
        _container is null
            ? Task.CompletedTask
            : _container.DisposeAsync().AsTask();

    /// <summary>컨테이너 연결 문자열을 사용하는 새 DbContext를 생성합니다.</summary>
    public GameDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<GameDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new GameDbContext(options);
    }
}
