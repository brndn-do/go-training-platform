namespace GoTrainingPlatform.Api;

/// <summary>
/// The key stores the host can be configured to use. Has no default: an unset or unrecognized
/// <c>DataProtection__Provider</c> fails to bind rather than falling through to one of these.
/// </summary>
public enum DataProtectionProvider
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
