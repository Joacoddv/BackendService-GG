using Xunit;

namespace GastroGestion.Api.Tests;

/// <summary>
/// Shared xUnit collection that ensures a single ApiFactory instance is used
/// across all integration test classes. This prevents concurrent factory
/// creation/disposal that would otherwise drop the test database mid-run.
/// </summary>
[CollectionDefinition(CollectionName)]
public sealed class IntegrationTestCollection : ICollectionFixture<ApiFactory>
{
    public const string CollectionName = "Integration";
}
