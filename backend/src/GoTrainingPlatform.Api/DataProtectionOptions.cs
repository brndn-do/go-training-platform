namespace GoTrainingPlatform.Api;

/// <summary>
/// Settings for where the Data Protection key ring is stored and how it is protected at rest.
/// </summary>
public sealed class DataProtectionOptions
{
  /// <summary>
  /// The configuration section these options bind from.
  /// </summary>
  public const string SectionName = "DataProtection";

  /// <summary>
  /// Gets or sets the key store to use. Bind as <c>DataProtection__Provider</c>.
  /// </summary>
  public DataProtectionProvider Provider { get; set; }

  /// <summary>
  /// Gets or sets the application name subkeys are derived from. Every host sharing a key
  /// ring must use the same value, or cookies issued by one are rejected by another.
  /// Bind as <c>DataProtection__ApplicationName</c>.
  /// </summary>
  public string ApplicationName { get; set; } = string.Empty;

  /// <summary>
  /// Gets or sets the directory the key ring is written to. Required by
  /// <see cref="DataProtectionProvider.FileSystem"/>, ignored otherwise.
  /// Bind as <c>DataProtection__KeyRingPath</c>.
  /// </summary>
  public string? KeyRingPath { get; set; }

  /// <summary>
  /// Gets or sets the absolute URI of the blob the key ring is written to. Names the blob
  /// itself, not its container; the container must already exist. Required by
  /// <see cref="DataProtectionProvider.AzureBlob"/>, ignored otherwise.
  /// Bind as <c>DataProtection__BlobUri</c>.
  /// </summary>
  public string? BlobUri { get; set; }

  /// <summary>
  /// Gets or sets the versionless identifier of the Key Vault key that encrypts the key ring.
  /// Required by <see cref="DataProtectionProvider.AzureBlob"/>, ignored otherwise.
  /// Bind as <c>DataProtection__KeyVaultKeyUri</c>.
  /// </summary>
  public string? KeyVaultKeyUri { get; set; }
}
