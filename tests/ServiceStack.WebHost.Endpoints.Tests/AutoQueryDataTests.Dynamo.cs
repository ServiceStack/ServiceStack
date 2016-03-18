using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.DynamoDBv2;
using Funq;
using NUnit.Framework;
using ServiceStack.Aws.DynamoDb;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class AutoQueryDataDynamoTests : AutoQueryDataTests
    {
        public override ServiceStackHost CreateAppHost()
        {
            return new AutoQueryDataDynamoAppHost();
        }
    }

    public class AutoQueryDataDynamoAppHost : AutoQueryDataAppHost
    {
        public override void Configure(Container container)
        {
            base.Configure(container);

            container.Register(c => new PocoDynamo(
                new AmazonDynamoDBClient("keyId", "key", new AmazonDynamoDBConfig {
                    ServiceURL = "http://localhost:8000",
                }))
                .RegisterTable<Rockstar>()
                .RegisterTable<Adhoc>()
                .RegisterTable<Movie>()
                .RegisterTable<AllFields>()
                .RegisterTable<PagingTest>()
            );

            var dynamo = container.Resolve<IPocoDynamo>();
            dynamo.InitSchema();
            dynamo.PutItems(SeedRockstars);
            dynamo.PutItems(SeedAdhoc);
            dynamo.PutItems(SeedMovies);
            dynamo.PutItems(SeedAllFields);
            dynamo.PutItems(SeedPagingTest);

            var feature = this.GetPlugin<AutoQueryDataFeature>();
            feature.AddDataSource(ctx => ctx.DynamoDbSource<Rockstar>());
            feature.AddDataSource(ctx => ctx.DynamoDbSource<Adhoc>());
            feature.AddDataSource(ctx => ctx.DynamoDbSource<Movie>());
            feature.AddDataSource(ctx => ctx.DynamoDbSource<AllFields>());
            feature.AddDataSource(ctx => ctx.DynamoDbSource<PagingTest>());
        }
    }

    public static class DynamoExtensions
    {
        public static QueryDataSource<T> DynamoDbSource<T>(this QueryDataContext ctx, IPocoDynamo dynamo = null)
        {
            if (dynamo == null)
                dynamo = HostContext.TryResolve<IPocoDynamo>();

            return new DynamoTableDataSource<T>(ctx, dynamo);
        }
    }

    internal class DynamoExpression
    {
        public string Expression;
        public Dictionary<string, object> Args;
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

    public class DynamoTableDataSource<T> : QueryDataSource<T>
    {
        private readonly IPocoDynamo db;
        private readonly DynamoMetadataType modelDef;

        public DynamoTableDataSource(QueryDataContext context, IPocoDynamo dynamo)
            : base(context)
        {
            this.db = dynamo;
            this.modelDef = db.GetTableMetadata<T>();
        }

        private IEnumerable<T> cache;
        private int? total;

        public override int Count(IDataQuery q)
        {
            return total.GetValueOrDefault(cache.Count());
        }

        public override IEnumerable<T> ApplyLimits(IEnumerable<T> source, int? skip, int? take)
        {
            return source; //ignore, limits added in GetResults();
        }

        public IEnumerable<T> GetResults(ScanExpression scanExpr, int? skip = null, int? take = null)
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

        public IEnumerable<T> GetResults(QueryExpression queryExpr, int? skip = null, int? take = null)
        {
            if (take != null)
                queryExpr.Limit = take.Value;

            return db.Query(queryExpr, r =>
            {
                if (total == null)
                    total = r.Count;
                return r.ConvertAll<T>();
            });
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
                var scanExpr = db.FromScan<T>();
                AddConditions(scanExpr, q);
                return cache = GetResults(scanExpr, q.Offset, q.Rows);
            }

            var rangeCondition = modelDef.RangeKey != null
                ? q.Conditions.FirstOrDefault(x =>
                    x.Field.Name.EqualsIgnoreCase(modelDef.RangeKey.Name))
                : null;

            var queryExpr = db.FromQuery<T>();

            var args = new Dictionary<string, object>();
            var hashFmt = DynamoQueryConditions.GetExpressionFormat(keyCondition.QueryCondition.Alias);
            var dynamoFmt = string.Format(hashFmt, queryExpr.GetFieldLabel(modelDef.HashKey.Name), ":k1");
            args["k1"] = keyCondition.Value;

            var rangeFmt = rangeCondition != null
                ? DynamoQueryConditions.GetExpressionFormat(rangeCondition.QueryCondition.Alias)
                : null;
            if (rangeFmt != null)
            {
                dynamoFmt += " AND " + string.Format(hashFmt, queryExpr.GetFieldLabel(modelDef.RangeKey.Name), ":k2");
                args["k2"] = rangeCondition.Value;
            }
            queryExpr.KeyCondition(dynamoFmt, args);

            AddConditions(queryExpr, q);

            return cache = GetResults(queryExpr, q.Offset, q.Rows);
        }

        public void AddConditions(IDynamoCommonQuery dynamoQ, IDataQuery q)
        {
            var dbConditions = new List<ConditionExpression>();
            var args = new Dictionary<string, object>();
            var sb = new StringBuilder();
            var isMultipleWithOrTerm = q.Conditions.Any(x => x.Term == QueryTerm.Or) 
                && q.Conditions.Count > 1;

            foreach (var condition in q.Conditions)
            {
                var fmt = DynamoQueryConditions.GetExpressionFormat(condition.QueryCondition.Alias);
                var multiFmt = DynamoQueryConditions.GetMultiExpressionFormat(condition.QueryCondition.Alias);

                if (fmt == null && multiFmt == null && isMultipleWithOrTerm)
                    throw new NotSupportedException("DynamoDB does not support {0} filter with multiple OR queries"
                        .Fmt(condition.QueryCondition.Alias));

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
                else if (condition.QueryCondition.Alias == ConditionAlias.Between)
                {
                    var values = ((IEnumerable)condition.Value).Map(x => x);
                    if (values.Count < 2)
                        throw new ArgumentException("{0} BETWEEN must have 2 values".Fmt(condition.Field.Name));

                    var pFrom = "p" + args.Count;
                    args[pFrom] = values[0];
                    var pTo = "p" + args.Count;
                    args[pTo] = values[1];

                    sb.Append(string.Format(multiFmt, dynamoQ.GetFieldLabel(condition.Field.Name), ":" + pFrom, ":" + pTo));
                }
                else
                {
                    var values = (IEnumerable)condition.Value;
                    var sbIn = new StringBuilder();
                    foreach (var value in values)
                    {
                        if (sbIn.Length > 0)
                            sbIn.Append(",");

                        var pArg = "p" + args.Count;
                        args[pArg] = value;

                        sbIn.Append(":" + pArg);
                    }

                    sb.Append(string.Format(multiFmt, dynamoQ.GetFieldLabel(condition.Field.Name), sbIn));
                }
            }

            if (sb.Length > 0)
            {
                dynamoQ.AddFilter(sb.ToString(), args);
            }

            q.Conditions.RemoveAll(dbConditions.Contains);
        }
    }
}