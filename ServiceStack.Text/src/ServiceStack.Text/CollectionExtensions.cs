using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ServiceStack;
public static class CollectionExtensions
{
    public static ICollection<T> CreateAndPopulate<T>(Type ofCollectionType, T[] withItems)
    {
        if (withItems == null)
            return null;

        if (ofCollectionType == null)
            return new List<T>(withItems);

        var genericType = ofCollectionType.FirstGenericType();
        var genericTypeDefinition = genericType?.GetGenericTypeDefinition();

        if (genericTypeDefinition == typeof(HashSet<>))
            return new HashSet<T>(withItems);

        if (genericTypeDefinition == typeof(LinkedList<>))
            return new LinkedList<T>(withItems);

        var collection = (ICollection<T>)ofCollectionType.CreateInstance();
        foreach (var item in withItems)
        {
            collection.Add(item);
        }
        return collection;
    }

    public static T[] ToArray<T>(this ICollection<T> collection)
    {
        var to = new T[collection.Count];
        collection.CopyTo(to, 0);
        return to;
    }

    public static object Convert<T>(object objCollection, Type toCollectionType)
    {
        var collection = (ICollection<T>) objCollection;
        var to = new T[collection.Count];
        collection.CopyTo(to, 0);
        return CreateAndPopulate(toCollectionType, to);
    }

    /// <summary>
    /// Return T[0] when enumerable is null, safe to use in enumerations like foreach
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<T> OrEmpty<T>(this IEnumerable<T> enumerable) => enumerable ?? [];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable OrEmpty(this IEnumerable enumerable) => enumerable ?? Array.Empty<object>();
}
