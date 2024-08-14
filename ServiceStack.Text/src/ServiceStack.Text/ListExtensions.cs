//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using ServiceStack.Text;
using ServiceStack.Text.Common;

namespace ServiceStack;

public static class ListExtensions
{
    public static string Join<T>(this IEnumerable<T> values)
    {
        return Join(values, JsWriter.ItemSeperatorString);
    }

    public static string Join<T>(this IEnumerable<T> values, string seperator)
    {
        var sb = StringBuilderThreadStatic.Allocate();
        foreach (var value in values)
        {
            if (sb.Length > 0)
                sb.Append(seperator);
            sb.Append(value);
        }
        return StringBuilderThreadStatic.ReturnAndFree(sb);
    }

    public static bool IsNullOrEmpty<T>(this List<T> list)
    {
        return list == null || list.Count == 0;
    }

    public static IEnumerable<TFrom> SafeWhere<TFrom>(this List<TFrom> list, Func<TFrom, bool> predicate)
    {
        return list == null 
            ? Array.Empty<TFrom>() 
            : list.Where(predicate);
    }

    public static int NullableCount<T>(this List<T> list) => list?.Count ?? 0;

    public static void AddIfNotExists<T>(this ICollection<T> list, T item)
    {
        if (!list.Contains(item))
            list.Add(item);
    }

    public static void AddDistinctRange<T>(this ICollection<T> list, IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            if (!list.Contains(item))
                list.Add(item);
        }
    }

    public static void AddDistinctRange<T>(this HashSet<T> set, IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            set.Add(item);
        }
    }

    public static void AddDistinctRanges<T>(this HashSet<T> set, params IEnumerable<T>[] collections)
    {
        foreach (var collection in collections)
        {
            foreach (var item in collection)
            {
                set.Add(item);
            }
        }
    }

    public static T[] NewArray<T>(this T[] array, T with = null, T without = null) where T : class
    {
        var to = new List<T>(array);

        if (with != null)
            to.Add(with);

        if (without != null)
            to.Remove(without);

        return to.ToArray();
    }

    public static List<T> InList<T>(this T value) => [value];

    public static T[] InArray<T>(this T value) => [value];

    public static List<Type> Add<T>(this List<Type> types)
    {
        types.Add(typeof(T));
        return types;
    }

    internal static IQueryable<TEntity> OrderBy<TEntity>(this IQueryable<TEntity> source, string field, string sortMethod)
    {
        var property = typeof(TEntity).GetProperty(field)
            ?? typeof(TEntity).GetProperties().FirstOrDefault(x => string.Equals(x.Name, field, StringComparison.OrdinalIgnoreCase));
        if (property == null)
            throw new MissingMemberException(typeof(TEntity).Name, field);
        var parameter = Expression.Parameter(typeof(TEntity), "p");
        var propertyAccess = Expression.MakeMemberAccess(parameter, property);
        var orderByExpression = Expression.Lambda(propertyAccess, parameter);
        var resultExpression = Expression.Call(typeof(Queryable), sortMethod, new[] { typeof(TEntity), property.PropertyType },
            source.Expression, Expression.Quote(orderByExpression));
        return source.Provider.CreateQuery<TEntity>(resultExpression);
    }

    public static IQueryable<TEntity> OrderBy<TEntity>(this IQueryable<TEntity> source, string sqlOrderByList)
    {
        var orderByFields = sqlOrderByList.Trim().Split(',');
        IQueryable<TEntity> result = source;
        var first = false;
        foreach (var item in orderByFields)
        {
            var field = item.Trim();
            if (string.IsNullOrEmpty(field)) continue;
            var desc = field.StartsWith("-");
            if (desc)
                field = field.Substring(1);

            var sortMethod = first ?
                desc ? "ThenByDescending" : "ThenBy" : 
                desc ? "OrderByDescending" : "OrderBy";

            result = result.OrderBy(field, sortMethod);
            first = true;
        }
        return result;
    }    
}