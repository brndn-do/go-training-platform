namespace Engine.Api.Tests;

/// <summary>
/// xUnit collection definition serializing every test class that starts a real KataGo
/// process. Running two of such classes concurrently has been enough for the kernel to
/// OOM-kill one of them.
/// </summary>
[CollectionDefinition("KataGo")]
public sealed class KataGoCollection
{
}
