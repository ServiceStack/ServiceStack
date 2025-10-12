// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using ServiceStack.Text;

namespace ServiceStack.Aws.DynamoDb;

public static class PocoDynamoExtensions
{
    public static IPocoDynamo RegisterTable<T>(this IPocoDynamo db)
    {
        DynamoMetadata.RegisterTable(typeof(T));
        return db;
    }

    public static IPocoDynamo RegisterTable(this IPocoDynamo db, Type tableType)
    {
        DynamoMetadata.RegisterTable(tableType);
        return db;
    }

    public static IPocoDynamo RegisterTables(this IPocoDynamo db, IEnumerable<Type> tableTypes)
    {
        DynamoMetadata.RegisterTables(tableTypes);
        return db;
    }

    public static void AddValueConverter(this IPocoDynamo db, Type type, IAttributeValueConverter valueConverter)
    {
        DynamoMetadata.Converters.ValueConverters[type] = valueConverter;
    }

    public static ITable GetTableSchema<T>(this IPocoDynamo db)
    {
        return db.GetTableSchema(typeof(T));
    }

    public static DynamoMetadataType GetTableMetadata<T>(this IPocoDynamo db)
    {
        return db.GetTableMetadata(typeof(T));
    }

    public static bool CreateTableIfMissing<T>(this IPocoDynamo db)
    {
        var table = db.GetTableMetadata<T>();
        return db.CreateMissingTables([table]);
    }

    public static async Task<bool> CreateTableIfMissingAsync<T>(this IPocoDynamo db, CancellationToken token=default)
    {
        var table = db.GetTableMetadata<T>();
        return await db.CreateMissingTablesAsync([table], token);
    }

    public static bool CreateTableIfMissing(this IPocoDynamo db, DynamoMetadataType table)
    {
        return db.CreateMissingTables([table]);
    }

    public static async Task<bool> CreateTableIfMissingAsync(this IPocoDynamo db, DynamoMetadataType table, CancellationToken token=default)
    {
        return await db.CreateMissingTablesAsync([table], token).ConfigAwait();
    }

    public static bool DeleteTable<T>(this IPocoDynamo db, TimeSpan? timeout = null)
    {
        var table = db.GetTableMetadata<T>();
        return db.DeleteTables([table.Name], timeout);
    }

    public static async Task<bool> DeleteTableAsync<T>(this IPocoDynamo db, TimeSpan? timeout = null, CancellationToken token=default)
    {
        var table = db.GetTableMetadata<T>();
        return await db.DeleteTablesAsync([table.Name], timeout, token).ConfigAwait();
    }

    public static bool CreateTable<T>(this IPocoDynamo db, TimeSpan? timeout = null)
    {
        var table = db.GetTableMetadata<T>();
        return db.CreateTables([table], timeout);
    }

    public static async Task<bool> CreateTableAsync<T>(this IPocoDynamo db, TimeSpan? timeout = null, CancellationToken token=default)
    {
        var table = db.GetTableMetadata<T>();
        return await db.CreateTablesAsync([table], timeout, token).ConfigAwait();
    }

    public static long DecrementById<T>(this IPocoDynamo db, object id, string fieldName, long amount = 1)
    {
        return db.Increment<T>(id, fieldName, amount * -1);
    }

    public static async Task<long> DecrementByIdAsync<T>(this IPocoDynamo db, object id, string fieldName, long amount = 1, CancellationToken token=default)
    {
        return await db.IncrementAsync<T>(id, fieldName, amount * -1, token).ConfigAwait();
    }

    public static long IncrementById<T>(this IPocoDynamo db, object id, Expression<Func<T, object>> fieldExpr, long amount = 1)
    {
        return db.Increment<T>(id, ExpressionUtils.GetMemberName(fieldExpr), amount);
    }

    public static async Task<long> IncrementByIdAsync<T>(this IPocoDynamo db, object id, Expression<Func<T, object>> fieldExpr, long amount = 1, CancellationToken token=default)
    {
        return await db.IncrementAsync<T>(id, ExpressionUtils.GetMemberName(fieldExpr), amount, token).ConfigAwait();
    }

    public static long DecrementById<T>(this IPocoDynamo db, object id, Expression<Func<T, object>> fieldExpr, long amount = 1)
    {
        return db.Increment<T>(id, ExpressionUtils.GetMemberName(fieldExpr), amount * -1);
    }

    public static async Task<long> DecrementByIdAsync<T>(this IPocoDynamo db, object id, Expression<Func<T, object>> fieldExpr, long amount = 1, CancellationToken token=default)
    {
        return await db.IncrementAsync<T>(id, ExpressionUtils.GetMemberName(fieldExpr), amount * -1, token).ConfigAwait();
    }

    public static List<T> GetAll<T>(this IPocoDynamo db)
    {
        return db.ScanAll<T>().ToList();
    }

    public static async Task<List<T>> GetAllAsync<T>(this IPocoDynamo db, CancellationToken token=default)
    {
        return await db.ScanAllAsync<T>(token).ToListAsync(token);
    }

    public static T GetItem<T>(this IPocoDynamo db, DynamoId id)
    {
        return db.GetItem<T>(id.Hash, id.Range);
    }

    public static async Task<T> GetItemAsync<T>(this IPocoDynamo db, DynamoId id, CancellationToken token=default)
    {
        return await db.GetItemAsync<T>(id.Hash, id.Range, token).ConfigAwait();
    }

    public static List<T> GetItems<T>(this IPocoDynamo db, IEnumerable<int> ids)
    {
        return db.GetItems<T>(ids.Map(x => (object)x));
    }

    public static async Task<List<T>> GetItemsAsync<T>(this IPocoDynamo db, IEnumerable<int> ids, CancellationToken token=default)
    {
        return await db.GetItemsAsync<T>(ids.Map(x => (object)x), token).ConfigAwait();
    }

    public static List<T> GetItems<T>(this IPocoDynamo db, IEnumerable<long> ids)
    {
        return db.GetItems<T>(ids.Map(x => (object)x));
    }

    public static async Task<List<T>> GetItemsAsync<T>(this IPocoDynamo db, IEnumerable<long> ids, CancellationToken token=default)
    {
        return await db.GetItemsAsync<T>(ids.Map(x => (object)x), token).ConfigAwait();
    }

    public static List<T> GetItems<T>(this IPocoDynamo db, IEnumerable<string> ids)
    {
        return db.GetItems<T>(ids.Map(x => (object)x));
    }

    public static async Task<List<T>> GetItemsAsync<T>(this IPocoDynamo db, IEnumerable<string> ids, CancellationToken token=default)
    {
        return await db.GetItemsAsync<T>(ids.Map(x => (object)x), token).ConfigAwait();
    }

    public static void DeleteItems<T>(this IPocoDynamo db, IEnumerable<int> ids)
    {
        db.DeleteItems<T>(ids.Map(x => (object)x));
    }

    public static async Task DeleteItemsAsync<T>(this IPocoDynamo db, IEnumerable<int> ids, CancellationToken token=default)
    {
        await db.DeleteItemsAsync<T>(ids.Map(x => (object) x), token).ConfigAwait();
    }

    public static void DeleteItems<T>(this IPocoDynamo db, IEnumerable<long> ids)
    {
        db.DeleteItems<T>(ids.Map(x => (object)x));
    }

    public static async Task DeleteItemsAsync<T>(this IPocoDynamo db, IEnumerable<long> ids, CancellationToken token=default)
    {
        await db.DeleteItemsAsync<T>(ids.Map(x => (object)x), token).ConfigAwait();
    }

    public static void DeleteItems<T>(this IPocoDynamo db, IEnumerable<string> ids)
    {
        db.DeleteItems<T>(ids.Map(x => (object)x));
    }

    public static async Task DeleteItemsAsync<T>(this IPocoDynamo db, IEnumerable<string> ids, CancellationToken token=default)
    {
        await db.DeleteItemsAsync<T>(ids.Map(x => (object)x), token).ConfigAwait();
    }

    public static void UpdateItem<T>(this IPocoDynamo db, object hash, object range = null,
        Expression<Func<T>> put = null,
        Expression<Func<T>> add = null,
        Func<T, object> delete = null)
    {
        db.UpdateItem<T>(new DynamoUpdateItem
        {
            Hash = hash,
            Range = range,
            Put = put.AssignedValues(),
            Add = add.AssignedValues(),
            Delete = delete.ToObjectKeys().ToArraySafe(),
        });
    }

    public static async Task UpdateItemAsync<T>(this IPocoDynamo db, object hash, object range = null,
        Expression<Func<T>> put = null,
        Expression<Func<T>> add = null,
        Func<T, object> delete = null, CancellationToken token=default)
    {
        await db.UpdateItemAsync<T>(new DynamoUpdateItem
        {
            Hash = hash,
            Range = range,
            Put = put.AssignedValues(),
            Add = add.AssignedValues(),
            Delete = delete.ToObjectKeys().ToArraySafe(),
        }, token).ConfigAwait();
    }

    internal static T[] ToArraySafe<T>(this IEnumerable<T> items)
    {
        return items?.ToArray();
    }

    public static Dictionary<string, object> AssignedValue<T>(this Func<T, object> fn)
    {
        if (fn == null)
            return null;

        var instance = typeof(T).CreateInstance<T>();
        var result = fn(instance);
        if (result == null || result.Equals(result.GetType().GetDefaultValue()))
            throw new ArgumentException("Cannot use Assinged Value Expression on null or default values");

        foreach (var entry in instance.ToObjectDictionary())
        {
            if (result.Equals(entry.Value))
                return new Dictionary<string, object> { { entry.Key, entry.Value } };
        }

        throw new ArgumentException("Could not find AssignedValue");
    }

    public static IEnumerable<string> ToObjectKeys<T>(this Func<T, object> fn)
    {
        if (fn == null)
            return null;

        var instance = typeof(T).CreateInstance<T>();
        var result = fn(instance);

        return result.ToObjectDictionary().Keys;
    }

    public static IEnumerable<T> ScanInto<T>(this IPocoDynamo db, ScanExpression request)
    {
        return db.Scan<T>(request.Projection<T>());
    }

    public static async Task<List<T>> ScanIntoAsync<T>(this IPocoDynamo db, ScanExpression request, CancellationToken token=default)
    {
        return await db.ScanAsync<T>(request.Projection<T>(), token).ToListAsync(token);
    }

    public static List<T> ScanInto<T>(this IPocoDynamo db, ScanExpression request, int limit)
    {
        return db.Scan<T>(request.Projection<T>(), limit: limit);
    }

    public static async Task<List<T>> ScanIntoAsync<T>(this IPocoDynamo db, ScanExpression request, int limit, CancellationToken token=default)
    {
        return await db.ScanAsync<T>(request.Projection<T>(), limit: limit, token: token).ToListAsync(token);
    }

    public static IEnumerable<T> QueryInto<T>(this IPocoDynamo db, QueryExpression request)
    {
        return db.Query<T>(request.Projection<T>());
    }

    public static async Task<List<T>> QueryIntoAsync<T>(this IPocoDynamo db, QueryExpression request, CancellationToken token=default)
    {
        return await db.QueryAsync<T>(request.Projection<T>(), token).ToListAsync(token);
    }

    public static List<T> QueryInto<T>(this IPocoDynamo db, QueryExpression request, int limit)
    {
        return db.Query<T>(request.Projection<T>(), limit: limit);
    }

    public static async Task<List<T>> QueryIntoAsync<T>(this IPocoDynamo db, QueryExpression request, int limit, CancellationToken token=default)
    {
        return await db.QueryAsync<T>(request.Projection<T>(), limit: limit, token).ToListAsync(token);
    }

    static readonly AttributeValue NullValue = new AttributeValue { NULL = true };

    public static Dictionary<string, AttributeValue> ToExpressionAttributeValues(this IPocoDynamo db, Dictionary<string, object> args)
    {
        var attrValues = new Dictionary<string, AttributeValue>();
        foreach (var arg in args)
        {
            var key = arg.Key.StartsWith(":")
                ? arg.Key
                : ":" + arg.Key;

            attrValues[key] = ToAttributeValue(db, arg.Value);
        }
        return attrValues;
    }

    internal static AttributeValue ToAttributeValue(this IPocoDynamo db, object value)
    {
        if (value == null)
            return NullValue;

        var argType = value.GetType();
        var dbType = db.Converters.GetFieldType(argType);

        return db.Converters.ToAttributeValue(db, argType, dbType, value);
    }
}