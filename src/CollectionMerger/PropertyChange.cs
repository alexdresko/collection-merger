namespace CollectionMerger;

public sealed class PropertyChange
{
	public PropertyChange(string propertyName, object? oldValue, object? newValue)
	{
		PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
		OldValue = oldValue;
		NewValue = newValue;
	}

	public string PropertyName { get; }
	public object? OldValue { get; }
	public object? NewValue { get; }
}
