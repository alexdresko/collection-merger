using System.Reflection;

namespace CollectionMerger;

internal static class StateCapture
{
    internal static Dictionary<string, object?> Capture<T>(T obj)
    {
        var state = new Dictionary<string, object?>();
        if (obj is null)
            return state;

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in properties)
        {
            if (!prop.CanRead)
                continue;

            var isEnumerable = typeof(System.Collections.IEnumerable).IsAssignableFrom(prop.PropertyType);
            var isString = prop.PropertyType == typeof(string);
            if (isEnumerable && !isString)
                continue;

            state[prop.Name] = prop.GetValue(obj);
        }

        return state;
    }

    internal static List<PropertyChange> DetectChanges(
        Dictionary<string, object?> before,
        Dictionary<string, object?> after)
    {
        var changes = new List<PropertyChange>();

        foreach (var (key, oldValue) in before)
        {
            if (!after.TryGetValue(key, out var newValue))
                continue;

            if (Equals(oldValue, newValue))
                continue;

            changes.Add(new PropertyChange(key, oldValue, newValue));
        }

        return changes;
    }
}
