namespace GoTrainingPlatform.Infrastructure.Tests;

/// <summary>
/// xUnit collection definition sharing one <see cref="PostgresFixture"/> (and
/// its container) across every test class in the <c>"Postgres"</c> collection,
/// instead of paying container-startup cost per test class.
/// </summary>
[CollectionDefinition("Postgres")]
public class PostgresCollection : ICollectionFixture<PostgresFixture>
{
}
