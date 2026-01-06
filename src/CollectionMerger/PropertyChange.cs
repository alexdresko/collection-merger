namespace CollectionMerger;

/// <summary>
/// Represents a change to a single property of an object.
/// </summary>
public sealed class PropertyChange {
    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyChange"/> class.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    /// <param name="oldValue">The old value of the property.</param>
    /// <param name="newValue">The new value of the property.</param>
    public PropertyChange(string propertyName, object? oldValue, object? newValue) {
        PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
        OldValue = oldValue;
        NewValue = newValue;
    }

    /// <summary>
    /// Gets the name of the property that changed.
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// Gets the old value of the property.
    /// </summary>
    public object? OldValue { get; }

    /// <summary>
    /// Gets the new value of the property.
    /// </summary>
    public object? NewValue { get; }
}
