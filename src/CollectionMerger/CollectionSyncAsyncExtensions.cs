namespace CollectionMerger;

public static class CollectionSyncAsyncExtensions
{
    /// <summary>
    /// Asynchronously merges <paramref name="source"/> into <paramref name="destination"/> and returns a report describing adds/updates/removes.
    /// </summary>
    public static async Task<SyncReport> MapFromAsync<TSource, TDestination>(
        this ICollection<TDestination> destination,
        IReadOnlyCollection<TSource> source,
        Func<TSource, TDestination, Task<bool>> matchPredicate,
        Func<TSource, TDestination, Mapper, Task> mapProperties)
        where TDestination : new()
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(matchPredicate);
        ArgumentNullException.ThrowIfNull(mapProperties);

        var mapper = new Mapper();

        await MapFromInternalAsync(
            destination,
            source,
            matchPredicate,
            mapProperties,
            mapper,
            parentPath: null,
            collectionName: typeof(TDestination).Name);

        return mapper.GetReport();
    }

    /// <summary>
    /// Asynchronously merges <paramref name="source"/> into <paramref name="destination"/> and returns a report describing adds/updates/removes.
    /// </summary>
    public static async Task<SyncReport> MapFromAsync<TSource, TDestination>(
        this ICollection<TDestination> destination,
        IEnumerable<TSource> source,
        Func<TSource, TDestination, Task<bool>> matchPredicate,
        Func<TSource, TDestination, Mapper, Task> mapProperties)
        where TDestination : new()
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(matchPredicate);
        ArgumentNullException.ThrowIfNull(mapProperties);

        var materializedSource = source as IReadOnlyCollection<TSource> ?? source.ToList();
        return await destination.MapFromAsync(materializedSource, matchPredicate, mapProperties);
    }

    /// <summary>
    /// Asynchronously merges <paramref name="source"/> into <paramref name="destination"/> as a nested operation, using an existing mapper context.
    /// </summary>
    public static async Task MapFromAsync<TSource, TDestination>(
        this ICollection<TDestination> destination,
        Mapper parent,
        IReadOnlyCollection<TSource> source,
        Func<TSource, TDestination, Task<bool>> matchPredicate,
        Func<TSource, TDestination, Mapper, Task> mapProperties)
        where TDestination : new()
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(matchPredicate);
        ArgumentNullException.ThrowIfNull(mapProperties);

        await MapFromInternalAsync(
            destination,
            source,
            matchPredicate,
            mapProperties,
            parent,
            parentPath: parent.CurrentPath,
            collectionName: typeof(TDestination).Name);
    }

    /// <summary>
    /// Asynchronously merges <paramref name="source"/> into <paramref name="destination"/> as a nested operation, using an existing mapper context.
    /// </summary>
    public static async Task MapFromAsync<TSource, TDestination>(
        this ICollection<TDestination> destination,
        Mapper parent,
        IEnumerable<TSource> source,
        Func<TSource, TDestination, Task<bool>> matchPredicate,
        Func<TSource, TDestination, Mapper, Task> mapProperties)
        where TDestination : new()
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(matchPredicate);
        ArgumentNullException.ThrowIfNull(mapProperties);

        var materializedSource = source as IReadOnlyCollection<TSource> ?? source.ToList();
        await destination.MapFromAsync(parent, materializedSource, matchPredicate, mapProperties);
    }

    private static async Task MapFromInternalAsync<TSource, TDestination>(
        ICollection<TDestination> destination,
        IReadOnlyCollection<TSource> source,
        Func<TSource, TDestination, Task<bool>> matchPredicate,
        Func<TSource, TDestination, Mapper, Task> mapProperties,
        Mapper mapper,
        string? parentPath,
        string collectionName)
        where TDestination : new()
    {
        foreach (var sourceItem in source)
        {
            var destItem = default(TDestination);
            foreach (var d in destination)
            {
                if (await matchPredicate(sourceItem, d))
                {
                    destItem = d;
                    break;
                }
            }

            if (destItem is not null)
            {
                var beforeState = StateCapture.Capture(destItem);

                var itemPath = PathBuilder.Build(parentPath, collectionName, sourceItem, destItem);
                mapper.PushPath(itemPath);

                await mapProperties(sourceItem, destItem, mapper);

                mapper.PopPath();

                var afterState = StateCapture.Capture(destItem);
                var changes = StateCapture.DetectChanges(beforeState, afterState);
                if (changes.Count > 0)
                {
                    mapper.RecordUpdate(itemPath, destItem!, changes);
                }

                continue;
            }

            var newItem = new TDestination();
            var newItemPath = PathBuilder.Build(parentPath, collectionName, sourceItem, newItem);
            mapper.PushPath(newItemPath);

            await mapProperties(sourceItem, newItem, mapper);

            mapper.PopPath();

            destination.Add(newItem);
            mapper.RecordAdd(newItemPath, newItem);
        }

        var toRemove = new List<TDestination>();
        foreach (var dest in destination)
        {
            var hasMatch = false;
            foreach (var src in source)
            {
                if (await matchPredicate(src, dest))
                {
                    hasMatch = true;
                    break;
                }
            }
            if (!hasMatch)
            {
                toRemove.Add(dest);
            }
        }

        foreach (var item in toRemove)
        {
            destination.Remove(item);
            var itemPath = PathBuilder.Build(parentPath, collectionName, sourceItem: null, destinationItem: item);
            mapper.RecordRemove(itemPath, item!);
        }
    }
}
