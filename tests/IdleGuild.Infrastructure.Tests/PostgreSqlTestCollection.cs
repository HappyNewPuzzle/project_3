namespace IdleGuild.Infrastructure.Tests;

/// <summary>Infrastructure 테스트가 하나의 PostgreSQL 수명 주기와 스키마를 공유하게 합니다.</summary>
[CollectionDefinition(Name)]
public sealed class PostgreSqlTestCollection :
    ICollectionFixture<PostgreSqlDatabaseFixture>
{
    public const string Name = "PostgreSQL integration";
}
