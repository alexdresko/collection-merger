namespace CollectionMerger;

public sealed class ChangeRecord {
    public ChangeRecord(ChangeType changeType, string path, object item, List<PropertyChange>? propertyChanges) {
        ChangeType = changeType;
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Item = item ?? throw new ArgumentNullException(nameof(item));
        PropertyChanges = propertyChanges;
    }

    public ChangeType ChangeType { get; }
    public string Path { get; }
    public object Item { get; }
    public IReadOnlyList<PropertyChange>? PropertyChanges { get; }
}
