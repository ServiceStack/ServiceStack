using System.Collections;

namespace ServiceStack.Blazor;

public static class EnumerableUtils
{
    public static bool IsEmpty(this ICollection? collection) => collection == null || collection.Count == 0;
    public static bool IsEmpty<T>(this T[]? collection) => collection == null || collection.Length == 0;
    public static bool IsEmpty<T>(this List<T>? collection) => collection == null || collection.Count == 0;
    public static T[] EmptyIfNull<T>(this T[]? collection) => collection ?? Array.Empty<T>();
    public static List<T> EmptyIfNull<T>(this List<T>? collection) => collection ?? TypeConstants<T>.EmptyList;

    public static List<object>? AsList(this object? items) => (items as IEnumerable)?.AsList();
    public static List<string>? AsStringList(this object? items) => items is IEnumerable e
        ? e.Map(x => x.ToString()!) 
        : null;
    public static List<object>? AsList(this IEnumerable? items) => items is IEnumerable e
        ? e.Map(x => x)
        : null;
    public static List<T> AsList<T>(this IEnumerable<T>? items) => items.Map(x => x);

    public static List<To> Map<To, From>(this IEnumerable<From>? items, Func<From, To> converter)
    {
        if (items == null)
            return new List<To>();

        var list = new List<To>();
        foreach (var item in items)
        {
            list.Add(converter(item));
        }
        return list;
    }

    public static List<To> Map<To>(this IEnumerable? items, Func<object, To> converter)
    {
        if (items == null)
            return new List<To>();

        var list = new List<To>();
        foreach (var item in items)
        {
            list.Add(converter(item));
        }
        return list;
    }

    public static Type FirstElementType(this IEnumerable collection, string key)
    {
        var list = collection.Cast<object>().ToList();
        foreach (var o in list)
        {
            if (o is IEnumerable<KeyValuePair<string, object?>> d)
            {
                foreach (var entry in d)
                {
                    if (entry.Key != key) continue;
                    if (entry.Value == null) continue;
                    var entryType = entry.Value.GetType();
                    return Nullable.GetUnderlyingType(entryType) ?? entryType;
                }
            }
        }
        foreach (var o in list)
        {
            if (o is IEnumerable<KeyValuePair<string, object?>> d)
            {
                foreach (var entry in d)
                {
                    if (entry.Key.EqualsIgnoreCase(key)) continue;
                    if (entry.Value == null) continue;
                    var entryType = entry.Value.GetType();
                    return Nullable.GetUnderlyingType(entryType) ?? entryType;
                }
            }
        }
        return typeof(string);
    }

    public static T? GetIgnoreCase<T>(this IDictionary<string,T> map, string? name)
    {
        if (map == null || name == null) 
            return default;

        if (map.TryGetValue(name, out var value))
            return value;
        var altName = char.IsUpper(name[0])
            ? char.ToLower(name[0]) + name[1..]
            : char.ToUpper(name[0]) + name[1..];
        if (map.TryGetValue(altName, out value))
            return value;

        foreach (var entry in map)
        {
            if (string.Equals(entry.Key, name, StringComparison.OrdinalIgnoreCase))
                return entry.Value;
        }
        return default;
    }

    public static object? FirstOrDefault(IEnumerable? e)
    {
        if (e != null)
        {
            foreach (var item in e)
                return item;
        }
        return null;
    }

    public static object Count(IEnumerable? e)
    {
        if (e == null)
            return 0;

        var i = 0;
        foreach (var _ in e)
            i++;
        return i;
    }
}
