namespace CollectionMerger;

/// <summary>
/// Provides extension methods for merging collections with change tracking.
/// </summary>
public static class CollectionSyncExtensions {
    /// <summary>
    /// Merges <paramref name="source"/> into <paramref name="destination"/> and returns a report describing adds/updates/removes.
    /// </summary>
    public static SyncReport MapFrom<TSource, TDestination>(
        this ICollection<TDestination> destination,
        IReadOnlyCollection<TSource> source,
        Func<TSource, TDestination, bool> matchPredicate,
        Action<TSource, TDestination, Mapper> mapProperties,
        Func<TSource, bool>? isSourceDeleted = null,
        Action<TDestination>? deleteDestination = null)
        where TDestination : new() {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(matchPredicate);
        ArgumentNullException.ThrowIfNull(mapProperties);

        var mapper = new Mapper();

        MapFromInternal(
            destination,
            source,
            matchPredicate,
            mapProperties,
            mapper,
            isSourceDeleted,
            deleteDestination,
            parentPath: null,
            collectionName: typeof(TDestination).Name);

        return mapper.GetReport();
    }

    /// <summary>
    /// Merges <paramref name="source"/> into <paramref name="destination"/> and returns a report describing adds/updates/removes.
    /// </summary>
    public static SyncReport MapFrom<TSource, TDestination>(
        this ICollection<TDestination> destination,
        IEnumerable<TSource> source,
        Func<TSource, TDestination, bool> matchPredicate,
        Action<TSource, TDestination, Mapper> mapProperties,
        Func<TSource, bool>? isSourceDeleted = null,
        Action<TDestination>? deleteDestination = null)
        where TDestination : new() {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(matchPredicate);
        ArgumentNullException.ThrowIfNull(mapProperties);

        var materializedSource = source as IReadOnlyCollection<TSource> ?? source.ToList();
        return destination.MapFrom(materializedSource, matchPredicate, mapProperties, isSourceDeleted, deleteDestination);
    }

    /// <summary>
    /// Merges <paramref name="source"/> into <paramref name="destination"/> as a nested operation, using an existing mapper context.
    /// </summary>
    public static void MapFrom<TSource, TDestination>(
        this ICollection<TDestination> destination,
        Mapper parent,
        IReadOnlyCollection<TSource> source,
        Func<TSource, TDestination, bool> matchPredicate,
        Action<TSource, TDestination, Mapper> mapProperties,
        Func<TSource, bool>? isSourceDeleted = null,
        Action<TDestination>? deleteDestination = null)
        where TDestination : new() {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(matchPredicate);
        ArgumentNullException.ThrowIfNull(mapProperties);

        MapFromInternal(
            destination,
            source,
            matchPredicate,
            mapProperties,
            parent,
            isSourceDeleted,
            deleteDestination,
            parentPath: parent.CurrentPath,
            collectionName: typeof(TDestination).Name);
    }

    /// <summary>
    /// Merges <paramref name="source"/> into <paramref name="destination"/> as a nested operation, using an existing mapper context.
    /// </summary>
    public static void MapFrom<TSource, TDestination>(
        this ICollection<TDestination> destination,
        Mapper parent,
        IEnumerable<TSource> source,
        Func<TSource, TDestination, bool> matchPredicate,
        Action<TSource, TDestination, Mapper> mapProperties,
        Func<TSource, bool>? isSourceDeleted = null,
        Action<TDestination>? deleteDestination = null)
        where TDestination : new() {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(matchPredicate);
        ArgumentNullException.ThrowIfNull(mapProperties);

        var materializedSource = source as IReadOnlyCollection<TSource> ?? source.ToList();
        destination.MapFrom(parent, materializedSource, matchPredicate, mapProperties, isSourceDeleted, deleteDestination);
    }

    private static void MapFromInternal<TSource, TDestination>(
        ICollection<TDestination> destination,
        IReadOnlyCollection<TSource> source,
        Func<TSource, TDestination, bool> matchPredicate,
        Action<TSource, TDestination, Mapper> mapProperties,
        Mapper mapper,
        Func<TSource, bool>? isSourceDeleted,
        Action<TDestination>? deleteDestination,
        string? parentPath,
        string collectionName)
        where TDestination : new() {
        void ApplyDelete(object? sourceItem, TDestination destinationItem) {
            if (deleteDestination is null) {
                destination.Remove(destinationItem);
            } else {
                deleteDestination(destinationItem);
            }

            var itemPath = PathBuilder.Build(parentPath, collectionName, sourceItem, destinationItem);
            mapper.RecordRemove(itemPath, destinationItem!);
        }

        foreach (var sourceItem in source) {
            if (isSourceDeleted?.Invoke(sourceItem) == true) {
                var deletedItem = destination.FirstOrDefault(d => matchPredicate(sourceItem, d));
                if (deletedItem is null) {
                    continue;
                }

                ApplyDelete(sourceItem, deletedItem);
                continue;
            }

            var destItem = destination.FirstOrDefault(d => matchPredicate(sourceItem, d));
            if (destItem is not null) {
                var beforeState = StateCapture.Capture(destItem);

                var itemPath = PathBuilder.Build(parentPath, collectionName, sourceItem, destItem);
                mapper.PushPath(itemPath);

                mapProperties(sourceItem, destItem, mapper);

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

            mapProperties(sourceItem, newItem, mapper);

            mapper.PopPath();

            destination.Add(newItem);
            mapper.RecordAdd(newItemPath, newItem);
        }

        var toRemove = destination
            .Where(dest => !source.Any(src => matchPredicate(src, dest)))
            .ToList();

        foreach (var item in toRemove) {
            ApplyDelete(sourceItem: null, destinationItem: item);
        }
    }
}
