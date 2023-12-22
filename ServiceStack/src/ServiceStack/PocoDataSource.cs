#nullable enable

using ServiceStack.Web;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ServiceStack;

public static class PocoDataSource
{
    public static PocoDataSource<T> Create<T>(ICollection<T> items) => new(items, items.Count);
    public static PocoDataSource<T> Create<T>(IEnumerable<T> items, long idSequence) => new(items, idSequence);

    public static PocoDataSource<T> Create<T>(IEnumerable<T> items, Func<IEnumerable<T>, long> nextIdSequence) =>
        new(items, nextIdSequence(items));

    public static PocoDataSource<T> Create<T>(ICollection<T> items, Func<IEnumerable<T>, long> nextId) =>
        new(items, nextId(items));
}

/// <summary>
/// Provide a thread-safe CRUD wrapper around a collection of POCOs
/// </summary>
public class PocoDataSource<T>
{
    readonly List<T> items;
    long idSequence;

    readonly System.Reflection.PropertyInfo idProp;
    readonly GetMemberDelegate<T> idGetter;
    readonly SetMemberDelegate<T> idSetter;
    readonly object defaultValue;

    public PocoDataSource(IEnumerable<T> items, long nextIdSequence = 0)
    {
        this.items = new List<T>(items);
        this.idSequence = nextIdSequence;

        var checkPropExists = typeof(T).GetProperty("Id");
        if (checkPropExists == null)
            throw new ArgumentException($@"{typeof(T).Name} does not have an Id property", nameof(items));

        idProp = checkPropExists;
        idGetter = idProp.CreateGetter<T>();
        idSetter = idProp.CreateSetter<T>();
        defaultValue = idProp.PropertyType.GetDefaultValue();
    }

    /// <summary>
    /// Return next Id in sequence
    /// </summary>
    public long NextId() => Interlocked.Increment(ref idSequence);

    /// <summary>
    /// Returns a shallow copy of all items
    /// </summary>
    /// <returns></returns>
    public List<T> GetAll()
    {
        lock (items)
            return new List<T>(items);
    }

    /// <summary>
    /// Create and return all items in a MemoryDataSource
    /// </summary>
    public MemoryDataSource<T> ToDataSource(IQueryData dto, IRequest req) => new(GetAll(), dto, req);

    /// <summary>
    /// Add an existing item.
    /// Use NextId() to populate Item with Unique Id
    /// Use Save() to Replace existing Item with same Id when it exists 
    /// </summary>
    public T Add(T item)
    {
        lock (items)
            items.Add(item);
        return item;
    }

    /// <summary>
    /// Replace Item 
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public bool TryUpdate(T item) => TryUpdateById(item, idGetter(item));

    /// <summary>
    /// Replace existing Item with same Id 
    /// </summary>
    /// <returns>true if an existing item was found and replaced otherwise false</returns>
    public bool TryUpdateById(T item, object itemId)
    {
        lock (items)
        {
            var updateIndex = FindIndexById(itemId);
            if (updateIndex < 0)
                return false;

            items[updateIndex] = item;
            return true;
        }
    }

    /// <summary>
    /// Delete Item by Poco
    /// </summary>
    /// <returns>true if an item was deleted otherwise false</returns>
    public bool TryDelete(T item) => TryDeleteById(idGetter(item));

    /// <summary>
    /// Delete Item with same Id
    /// </summary>
    /// <returns>true if an item was deleted otherwise false</returns>
    public bool TryDeleteById(object itemId)
    {
        lock (items)
        {
            var updateIndex = FindIndexById(itemId);
            if (updateIndex < 0)
                return false;

            items.RemoveAt(updateIndex);
            return true;
        }
    }

    /// <summary>
    /// Delete All Item with matching Ids
    /// </summary>
    /// <returns>true if an item was deleted otherwise false</returns>
    public int TryDeleteByIds<TId>(IEnumerable<TId> itemIds)
    {
        lock (items)
        {
            var itemsRemoved = 0;
            foreach (var itemId in itemIds)
            {
                if (itemId == null)
                    continue;
                    
                var updateIndex = FindIndexById(itemId);
                if (updateIndex < 0)
                {
                    itemsRemoved++;
                    continue;
                }

                items.RemoveAt(updateIndex);
            }

            return itemsRemoved;
        }
    }

    // Always call within lock
    private int FindIndexById(object id)
    {
        for (int i = 0; i < items.Count; i++)
        {
            T el = items[i];
            var elId = idGetter(el);
            if (id.Equals(elId))
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Add or Update existing Item if item with same Id exists
    /// </summary>
    public T Save(T item)
    {
        var itemId = idGetter(item);
        if (itemId == defaultValue)
        {
            itemId = NextId().ConvertTo(idProp.PropertyType);
            idSetter(item, itemId);
            lock (items)
            {
                items.Add(item);
            }
        }
        else
        {
            lock (items)
            {
                var updateIndex = FindIndexById(itemId);
                if (updateIndex >= 0)
                    items[updateIndex] = item;
                else
                    items.Add(item);
            }
        }

        return item;
    }
}