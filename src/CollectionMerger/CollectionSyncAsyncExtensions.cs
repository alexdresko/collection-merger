namespace CollectionMerger;

/// <summary>
/// Provides asynchronous extension methods for merging collections with change tracking.
/// </summary>
public static class CollectionSyncAsyncExtensions {
    /// <summary>
    /// Asynchronously merges <paramref name="source"/> into <paramref name="destination"/> and returns a report describing adds/updates/removes.
    /// </summary>
    public static async Task<SyncReport> MapFromAsync<TSource, TDestination>(
        this ICollection<TDestination> destination,
        IReadOnlyCollection<TSource> source,
        Func<TSource, TDestination, Task<bool>> matchPredicate,
        Func<TSource, TDestination, Mapper, Task> mapProperties)
        where TDestination : new() {
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
        where TDestination : new() {
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
        where TDestination : new() {
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
        where TDestination : new() {
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
        where TDestination : new() {
        foreach (var sourceItem in source) {
            var destItem = await FindFirstMatchAsync(destination, sourceItem, matchPredicate);

            if (destItem is not null) {
                var beforeState = StateCapture.Capture(destItem);

                var itemPath = PathBuilder.Build(parentPath, collectionName, sourceItem, destItem);
                mapper.PushPath(itemPath);

                await mapProperties(sourceItem, destItem, mapper);

                mapper.PopPath();

                var afterState = StateCapture.Capture(destItem);
                var changes = StateCapture.DetectChanges(beforeState, afterState);
                if (changes.Count > 0) {
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

        var toRemove = await FindItemsToRemoveAsync(destination, source, matchPredicate);

        foreach (var item in toRemove) {
            destination.Remove(item);
            var itemPath = PathBuilder.Build(parentPath, collectionName, sourceItem: null, destinationItem: item);
            mapper.RecordRemove(itemPath, item!);
        }
    }

    private static async Task<TDestination?> FindFirstMatchAsync<TSource, TDestination>(
        ICollection<TDestination> destination,
        TSource sourceItem,
        Func<TSource, TDestination, Task<bool>> matchPredicate) {
        foreach (var d in destination) {
            if (await matchPredicate(sourceItem, d)) {
                return d;
            }
        }
        return default;
    }

    private static async Task<List<TDestination>> FindItemsToRemoveAsync<TSource, TDestination>(
        ICollection<TDestination> destination,
        IReadOnlyCollection<TSource> source,
        Func<TSource, TDestination, Task<bool>> matchPredicate) {
        var toRemove = new List<TDestination>();

        foreach (var dest in destination) {
            var hasMatch = await AnyMatchAsync(source, dest, matchPredicate);
            if (!hasMatch) {
                toRemove.Add(dest);
            }
        }

        return toRemove;
    }

    private static async Task<bool> AnyMatchAsync<TSource, TDestination>(
        IReadOnlyCollection<TSource> source,
        TDestination dest,
        Func<TSource, TDestination, Task<bool>> matchPredicate) {
        foreach (var src in source) {
            if (await matchPredicate(src, dest)) {
                return true;
            }
        }
        return false;
    }
}
