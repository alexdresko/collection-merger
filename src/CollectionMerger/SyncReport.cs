namespace CollectionMerger;

/// <summary>
/// Represents a report of all changes made during a collection merge operation.
/// </summary>
public sealed class SyncReport {
    /// <summary>
    /// Initializes a new instance of the <see cref="SyncReport"/> class.
    /// </summary>
    /// <param name="changes">The list of all changes recorded during the merge.</param>
    public SyncReport(IReadOnlyList<ChangeRecord> changes) {
        ArgumentNullException.ThrowIfNull(changes);
        Changes = changes;
    }

    /// <summary>
    /// Gets the list of all changes recorded during the merge.
    /// </summary>
    public IReadOnlyList<ChangeRecord> Changes { get; }

    /// <summary>
    /// Gets the total number of changes.
    /// </summary>
    public int TotalChanges => Changes.Count;

    /// <summary>
    /// Gets a value indicating whether any changes were made.
    /// </summary>
    public bool HasChanges => TotalChanges > 0;

    /// <summary>
    /// Gets the number of items that were updated.
    /// </summary>
    public int UpdatedCount => Changes.Count(c => c.ChangeType == ChangeType.Updated);

    /// <summary>
    /// Gets the number of items that were added.
    /// </summary>
    public int AddedCount => Changes.Count(c => c.ChangeType == ChangeType.Added);

    /// <summary>
    /// Gets the number of items that were removed.
    /// </summary>
    public int RemovedCount => Changes.Count(c => c.ChangeType == ChangeType.Removed);
}
