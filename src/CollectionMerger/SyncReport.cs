namespace CollectionMerger;

public sealed class SyncReport
{
	public SyncReport(IReadOnlyList<ChangeRecord> changes)
	{
		ArgumentNullException.ThrowIfNull(changes);
		Changes = changes;
	}

	public IReadOnlyList<ChangeRecord> Changes { get; }

	public int TotalChanges => Changes.Count;
	public int UpdatedCount => Changes.Count(c => c.ChangeType == ChangeType.Updated);
	public int AddedCount => Changes.Count(c => c.ChangeType == ChangeType.Added);
	public int RemovedCount => Changes.Count(c => c.ChangeType == ChangeType.Removed);
}
