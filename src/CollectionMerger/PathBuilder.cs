using System.Reflection;

namespace CollectionMerger;

internal static class PathBuilder
{
    internal static string Build(
        string? parentPath,
        string collectionName,
        object? sourceItem,
        object? destinationItem)
    {
        var id = GetIdString(sourceItem) ?? GetIdString(destinationItem) ?? "?";
        return string.IsNullOrEmpty(parentPath)
            ? $"{collectionName}[{id}]"
            : $"{parentPath}.{collectionName}[{id}]";
    }

    internal static string Build<TSource, TDestination>(
        string? parentPath,
        string collectionName,
        TSource? sourceItem,
        TDestination? destinationItem)
    {
        return Build(parentPath, collectionName, (object?)sourceItem, (object?)destinationItem);
    }

    private static string? GetIdString(object? item)
    {
        if (item is null)
            return null;

        var type = item.GetType();
        var idProp = type.GetProperty("ID", BindingFlags.Public | BindingFlags.Instance)
            ?? type.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);

        if (idProp is null || !idProp.CanRead)
            return null;

        return idProp.GetValue(item)?.ToString();
    }
}
