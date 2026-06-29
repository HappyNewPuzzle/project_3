using IdleGuild.Domain;

namespace IdleGuild.Domain.Tests;

/// <summary>도메인 계층이 외부 프로젝트에 의존하지 않는 구조를 보호합니다.</summary>
public sealed class DomainDependencyTests
{
    // 순수 게임 규칙 계층에서 다른 IdleGuild 프로젝트 참조가 생기면 실패합니다.
    [Fact]
    public void Domain_DoesNotReferenceOtherProjectLayers()
    {
        var projectReferences = typeof(DomainAssembly)
            .Assembly
            .GetReferencedAssemblies()
            .Where(reference =>
                reference.Name?.StartsWith("IdleGuild.", StringComparison.Ordinal) is true)
            .Select(reference => reference.Name)
            .ToArray();

        Assert.Empty(projectReferences);
    }
}
