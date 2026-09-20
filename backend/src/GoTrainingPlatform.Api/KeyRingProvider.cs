namespace GoTrainingPlatform.Api;

/// <summary>
/// The key stores the host can be configured to use. Numbered from 1, so an unset
/// <c>DataProtection__Provider</c> cannot silently mean one of these.
/// </summary>
public enum KeyRingProvider
{
  /// <summary>
  /// Azure Blob Storage, encrypted with a Key Vault key, authenticated with a managed identity.
  /// </summary>
  AzureBlob = 1,

  /// <summary>
  /// A directory on disk, unencrypted at rest.
  /// </summary>
  FileSystem = 2,

  /// <summary>
  /// Kept with the running instance and discarded with it. Sessions do not survive a restart.
  /// </summary>
  Ephemeral = 3,
}
