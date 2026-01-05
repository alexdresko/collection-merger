namespace CollectionMerger;

public sealed class Mapper
{
	private readonly Stack<string> _pathStack = new();
	private readonly List<ChangeRecord> _changes = new();

	public string? CurrentPath => _pathStack.Count > 0 ? _pathStack.Peek() : null;

	internal void PushPath(string path)
	{
		_pathStack.Push(path);
	}

	internal void PopPath()
	{
		if (_pathStack.Count > 0)
			_pathStack.Pop();
	}

	internal void RecordUpdate(string path, object item, List<PropertyChange> propertyChanges)
	{
		_changes.Add(new ChangeRecord(ChangeType.Updated, path, item, propertyChanges));
	}

	internal void RecordAdd(string path, object item)
	{
		_changes.Add(new ChangeRecord(ChangeType.Added, path, item, propertyChanges: null));
	}

	internal void RecordRemove(string path, object item)
	{
		_changes.Add(new ChangeRecord(ChangeType.Removed, path, item, propertyChanges: null));
	}

	internal SyncReport GetReport()
	{
		return new SyncReport(_changes);
	}
}
