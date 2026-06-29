using IdleGuild.Domain;

namespace IdleGuild.Domain.Tests;

public sealed class DomainDependencyTests
{
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
