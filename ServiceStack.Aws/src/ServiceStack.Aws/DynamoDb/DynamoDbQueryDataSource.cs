using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Text;

namespace ServiceStack.Aws.DynamoDb;

public static class DynamoQueryDataSourceExtensions
{
    public static QueryDataSource<T> DynamoDbSource<T>(this QueryDataContext ctx, IPocoDynamo dynamo = null, bool allowScans = true)
    {
        if (dynamo == null)
            dynamo = HostContext.TryResolve<IPocoDynamo>();

        return new DynamoDbQueryDataSource<T>(ctx, dynamo, allowScans);
    }
}

internal static class DynamoQueryConditions
{
    internal static string GetExpressionFormat(string operand)
    {
        switch (operand)
        {
            case ConditionAlias.Equals:
            case ConditionAlias.NotEqual:
            case ConditionAlias.GreaterEqual:
            case ConditionAlias.Greater:
            case ConditionAlias.LessEqual:
            case ConditionAlias.Less:
                return "{0} " + operand + " {1}";

            case ConditionAlias.StartsWith:
                return "begins_with({0}, {1})";

            default:
                return null;
        }
    }

    internal static string GetMultiExpressionFormat(string operand)
    {
        switch (operand)
        {
            case ConditionAlias.In:
                return "{0} IN ({1})";

            case ConditionAlias.Between:
                return "{0} BETWEEN {1} AND {2}";

            default:
                return null;
        }
    }
}
    
public class DynamoDbQueryDataSource<T> : QueryDataSource<T>
{
    protected readonly IPocoDynamo db;
    protected readonly DynamoMetadataType modelDef;
    protected bool isGlobalIndex;
    protected bool allowScans;

    public DynamoDbQueryDataSource(QueryDataContext context, IPocoDynamo dynamo, bool allowScans=true)
        : base(context)
    {
        this.db = dynamo;
        this.modelDef = db.GetTableMetadata<T>();
        isGlobalIndex = typeof(T).IsGlobalIndex();
        this.allowScans = allowScans;
    }

    protected IEnumerable<T> cache;
    protected int? total;

    public override int Count(IDataQuery q)
    {
        return total.GetValueOrDefault(cache.Count());
    }

    public override IEnumerable<T> ApplyLimits(IEnumerable<T> source, int? skip, int? take)
    {
        return source; //ignore, limits added in GetResults();
    }

    public virtual IEnumerable<T> GetResults(ScanExpression scanExpr, int? skip = null, int? take = null)
    {
        var results = db.Scan(scanExpr, r =>
        {
            if (total == null)
                total = r.Count;
            return r.ConvertAll<T>();
        });

        if (skip != null)
            results = results.Skip(skip.Value);

        if (take != null)
            results = results.Take(take.Value);

        return results.ToList();
    }

    public virtual IEnumerable<T> GetResults(QueryExpression queryExpr, int? skip = null, int? take = null)
    {
        var results = db.Query(queryExpr, r =>
        {
            if (total == null)
                total = r.Count;
            return r.ConvertAll<T>();
        });

        if (skip != null)
            results = results.Skip(skip.Value);

        if (take != null)
            results = results.Take(take.Value);

        return results.ToList();
    }

    public override IEnumerable<T> GetDataSource(IDataQuery q)
    {
        if (cache != null)
            return cache;

        var keyCondition = q.Conditions.FirstOrDefault(x =>
            x.Field != null && x.Field.Name.EqualsIgnoreCase(modelDef.HashKey.Name)
                            && x.QueryCondition.Alias == ConditionAlias.Equals);

        if (keyCondition == null)
        {
            var scanExpr = CreateScanExpresion();
            AddConditions(scanExpr, q);
            return cache = GetResults(scanExpr, q.Offset, q.Rows);
        }

        var rangeCondition = modelDef.RangeKey != null
            ? q.Conditions.FirstOrDefault(x =>
                x.Field != null && x.Field.Name.EqualsIgnoreCase(modelDef.RangeKey.Name))
            : null;
        var rangeField = rangeCondition != null
            ? modelDef.RangeKey.Name
            : null;

        var queryExpr = CreateQueryExpression();

        var args = new Dictionary<string, object>();
        var hashFmt = DynamoQueryConditions.GetExpressionFormat(keyCondition.QueryCondition.Alias);
        var dynamoFmt = string.Format(hashFmt, queryExpr.GetFieldLabel(modelDef.HashKey.Name), ":k0");
        args["k0"] = keyCondition.Value;

        if (rangeCondition == null)
        {
            foreach (var index in modelDef.LocalIndexes)
            {
                rangeCondition = q.Conditions.FirstOrDefault(x =>
                    x.Field != null && x.Field.Name.EqualsIgnoreCase(index.RangeKey.Name));

                if (rangeCondition != null)
                {
                    rangeField = index.RangeKey.Name;
                    queryExpr.IndexName = index.Name;
                    break;
                }
            }
        }

        if (rangeCondition != null)
        {
            var rangeFmt = DynamoQueryConditions.GetExpressionFormat(rangeCondition.QueryCondition.Alias);
            if (rangeFmt != null)
            {
                dynamoFmt += " AND " + string.Format(rangeFmt, queryExpr.GetFieldLabel(rangeField), ":k1");
                args["k1"] = rangeCondition.Value;
            }
            else
            {
                var multiFmt = DynamoQueryConditions.GetMultiExpressionFormat(rangeCondition.QueryCondition.Alias);
                if (multiFmt != null)
                {
                    var multiExpr = GetMultiConditionExpression(queryExpr, rangeCondition, multiFmt, args, argPrefix: "k");
                    dynamoFmt += " AND " + multiExpr;
                }
            }
        }

        queryExpr.KeyCondition(dynamoFmt, args);

        q.Conditions.RemoveAll(x => x == keyCondition || x == rangeCondition);

        AddConditions(queryExpr, q);

        return cache = GetResults(queryExpr, q.Offset, q.Rows);
    }

    public virtual QueryExpression<T> CreateQueryExpression()
    {
        return !isGlobalIndex
            ? db.FromQuery<T>()
            : db.FromQueryIndex<T>();
    }

    public virtual ScanExpression<T> CreateScanExpresion()
    {
        if (!allowScans)
            throw new InvalidOperationException($"AutoQuery SCAN is not permitted on '{modelDef.Name}' DynamoDB Table");

        return !isGlobalIndex
            ? db.FromScan<T>()
            : db.FromScanIndex<T>();
    }

    public virtual void AddConditions(IDynamoCommonQuery dynamoQ, IDataQuery q)
    {
        if (q.Conditions.Count == 0)
            return;

        var dbConditions = new List<DataConditionExpression>();
        var args = new Dictionary<string, object>();
        var sb = StringBuilderCache.Allocate();
        var isMultipleWithOrTerm = q.Conditions.Any(x => x.Term == QueryTerm.Or)
                                   && q.Conditions.Count > 1;

        foreach (var condition in q.Conditions)
        {
            var fmt = DynamoQueryConditions.GetExpressionFormat(condition.QueryCondition.Alias);
            var multiFmt = DynamoQueryConditions.GetMultiExpressionFormat(condition.QueryCondition.Alias);

            if (fmt == null && multiFmt == null && isMultipleWithOrTerm)
                throw new NotSupportedException(
                    $"DynamoDB does not support {condition.QueryCondition.Alias} filter with multiple OR queries");

            if (fmt == null && multiFmt == null)
                continue;

            dbConditions.Add(condition);

            if (sb.Length > 0)
                sb.Append(condition.Term == QueryTerm.Or ? " OR " : " AND ");

            if (fmt != null)
            {
                var pId = "p" + args.Count;
                args[pId] = condition.Value;

                sb.Append(string.Format(fmt, dynamoQ.GetFieldLabel(condition.Field.Name), ":" + pId));
            }
            else
            {
                var multiExpr = GetMultiConditionExpression(dynamoQ, condition, multiFmt, args);
                sb.Append(multiExpr);
            }
        }

        var filter = StringBuilderCache.ReturnAndFree(sb);
        if (filter.Length > 0)
        {
            dynamoQ.AddFilter(filter, args);
        }

        q.Conditions.RemoveAll(dbConditions.Contains);
    }

    public virtual string GetMultiConditionExpression(IDynamoCommonQuery dynamoQ, DataConditionExpression condition, string multiFmt, Dictionary<string, object> args, string argPrefix = "p")
    {
        if (multiFmt == null)
            return null;

        if (condition.QueryCondition.Alias == ConditionAlias.Between)
        {
            var values = ((IEnumerable)condition.Value).Map(x => x);
            if (values.Count < 2)
                throw new ArgumentException($"{condition.Field.Name} BETWEEN must have 2 values");

            var pFrom = argPrefix + args.Count;
            args[pFrom] = values[0];
            var pTo = argPrefix + args.Count;
            args[pTo] = values[1];

            return string.Format(multiFmt, dynamoQ.GetFieldLabel(condition.Field.Name), ":" + pFrom, ":" + pTo);
        }
        else
        {
            var values = (IEnumerable)condition.Value;
            var sbIn = StringBuilderCache.Allocate();
            foreach (var value in values)
            {
                if (sbIn.Length > 0)
                    sbIn.Append(",");

                var pArg = argPrefix + args.Count;
                args[pArg] = value;

                sbIn.Append(":" + pArg);
            }

            return string.Format(multiFmt, 
                dynamoQ.GetFieldLabel(condition.Field.Name), 
                StringBuilderCache.ReturnAndFree(sbIn));
        }
    }
}