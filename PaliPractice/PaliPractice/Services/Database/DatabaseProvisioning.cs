namespace PaliPractice.Services.Database;

/// <summary>
/// Status of a database provisioning operation.
/// </summary>
public enum DatabaseProvisioningStatus
{
    /// <summary>
    /// Writable database was created fresh.
    /// </summary>
    CreatedWritable,

    /// <summary>
    /// Read directly from app bundle (iOS, Desktop).
    /// </summary>
    ReusedBundled,

    /// <summary>
    /// Reused previously copied database.
    /// </summary>
    ReusedCopied,

    /// <summary>
    /// Copied from bundle to local storage.
    /// </summary>
    CopiedFromBundle,

    /// <summary>
    /// Database corruption was detected.
    /// </summary>
    CorruptionDetected,

    /// <summary>
    /// Corrupt database was deleted before re-copy.
    /// </summary>
    DeletedCorrupt,

    /// <summary>
    /// Failed to delete corrupt database.
    /// </summary>
    FailedToDeleteCorrupt,

    /// <summary>
    /// Generic failure during provisioning.
    /// </summary>
    Failed,

    /// <summary>
    /// Failed due to insufficient disk space.
    /// </summary>
    FailedDiskSpace
}

/// <summary>
/// Records a database provisioning event for diagnostics.
/// </summary>
public sealed record DatabaseProvisionedEvent(
    DatabaseFile Database,
    DatabaseProvisioningStatus Status,
    DateTimeOffset Timestamp,
    string Source,
    Exception? Exception = null)
{
    public bool IsFailure => Status >= DatabaseProvisioningStatus.Failed;

    public string Summary => Exception != null
        ? $"{Database.Name} failed in {Source}: {Exception.Message}"
        : $"{Database.Name} initialized via {Status} in {Source}";
}
