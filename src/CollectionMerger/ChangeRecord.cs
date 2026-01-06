namespace CollectionMerger;

/// <summary>
/// Represents a recorded change to a collection item (add, update, or remove).
/// </summary>
public sealed class ChangeRecord {
    /// <summary>
    /// Initializes a new instance of the <see cref="ChangeRecord"/> class.
    /// </summary>
    /// <param name="changeType">The type of change.</param>
    /// <param name="path">The path to the changed item.</param>
    /// <param name="item">The item that was changed.</param>
    /// <param name="propertyChanges">The list of property changes for updates, or null for adds/removes.</param>
    public ChangeRecord(ChangeType changeType, string path, object item, List<PropertyChange>? propertyChanges) {
        ChangeType = changeType;
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Item = item ?? throw new ArgumentNullException(nameof(item));
        PropertyChanges = propertyChanges;
    }

    /// <summary>
    /// Gets the type of change (Added, Updated, or Removed).
    /// </summary>
    public ChangeType ChangeType { get; }

    /// <summary>
    /// Gets the path to the changed item in the collection hierarchy.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the item that was changed.
    /// </summary>
    public object Item { get; }

    /// <summary>
    /// Gets the list of property changes for updates, or null for adds/removes.
    /// </summary>
    public IReadOnlyList<PropertyChange>? PropertyChanges { get; }
}
