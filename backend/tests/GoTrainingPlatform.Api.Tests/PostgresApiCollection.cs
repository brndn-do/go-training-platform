namespace GoTrainingPlatform.Api.Tests;

/// <summary>
/// xUnit collection definition sharing one <see cref="PostgresApiFixture"/> (and its
/// container and host) across every test class in the <c>"PostgresApi"</c> collection,
/// instead of paying that startup cost per test class.
/// </summary>
[CollectionDefinition("PostgresApi")]
public sealed class PostgresApiCollection : ICollectionFixture<PostgresApiFixture>
{
}
