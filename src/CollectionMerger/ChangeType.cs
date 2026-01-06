namespace CollectionMerger;

/// <summary>
/// Specifies the type of change made to a collection item.
/// </summary>
public enum ChangeType
{
    /// <summary>
    /// The item was added to the collection.
    /// </summary>
    Added,
    
    /// <summary>
    /// The item was updated in the collection.
    /// </summary>
    Updated,
    
    /// <summary>
    /// The item was removed from the collection.
    /// </summary>
    Removed
}
