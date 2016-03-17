using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.DynamoDBv2;
using Funq;
using NUnit.Framework;
using ServiceStack.Aws.DynamoDb;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [Ignore]
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
                case QueryConditionOperands.Equals:
                case QueryConditionOperands.NotEqual:
                case QueryConditionOperands.GreaterEqual:
                case QueryConditionOperands.Greater:
                case QueryConditionOperands.LessEqual:
                case QueryConditionOperands.Less:
                    return "{0} " + operand + " {1}";
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

        public IEnumerable<T> GetResults(ScanExpression scanExpr, int? take = null)
        {
            if (take != null)
                scanExpr.Limit = take.Value;

            return db.Scan(scanExpr, r =>
            {
                if (total == null)
                    total = r.Count;
                return r.ConvertAll<T>();
            });
        }

        public IEnumerable<T> GetResults(QueryExpression queryExpr, int? take = null)
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
                x.Field.Name.EqualsIgnoreCase(modelDef.HashKey.Name)
                && x.QueryCondition.Operand == QueryConditionOperands.Equals);

            if (keyCondition == null)
            {
                var scanExpr = db.FromScan<T>();
                AddConditions(scanExpr, q);
                return cache = q.Rows != null
                    ? GetResults(scanExpr, q.Rows.Value + q.Offset.GetValueOrDefault(0)) //Limit applied on resultset
                    : GetResults(scanExpr).ToList();
            }

            var rangeCondition = modelDef.RangeKey != null
                ? q.Conditions.FirstOrDefault(x =>
                    x.Field.Name.EqualsIgnoreCase(modelDef.RangeKey.Name))
                : null;

            var queryExpr = db.FromQuery<T>();

            var args = new Dictionary<string, object>();
            var hashFmt = DynamoQueryConditions.GetExpressionFormat(keyCondition.QueryCondition.Operand);
            var dynamoFmt = string.Format(hashFmt, queryExpr.GetFieldLabel(modelDef.HashKey.Name), ":k1");
            args["k1"] = keyCondition.Value;

            var rangeFmt = rangeCondition != null
                ? DynamoQueryConditions.GetExpressionFormat(rangeCondition.QueryCondition.Operand)
                : null;
            if (rangeFmt != null)
            {
                dynamoFmt += " AND " + string.Format(hashFmt, queryExpr.GetFieldLabel(modelDef.RangeKey.Name), ":k2");
                args["k2"] = rangeCondition.Value;
            }
            queryExpr.KeyCondition(dynamoFmt, args);

            AddConditions(queryExpr, q);

            return cache = q.Rows != null
                ? GetResults(queryExpr, q.Rows.Value + q.Offset.GetValueOrDefault(0)) //Limit applied on resultset
                : GetResults(queryExpr).ToList();
        }

        public void AddConditions(IDynamoCommonQuery dynamoQ, IDataQuery q)
        {
            var dbConditions = new List<ConditionExpression>();
            var args = new Dictionary<string, object>();
            var sb = new StringBuilder();

            foreach (var condition in q.Conditions)
            {
                var fmt = DynamoQueryConditions.GetExpressionFormat(condition.QueryCondition.Operand);
                if (fmt == null)
                    continue;

                dbConditions.Add(condition);

                if (sb.Length > 0)
                    sb.Append(condition.Term == QueryTerm.Or ? " OR " : " AND ");

                var pId = "p" + args.Count;
                args[pId] = condition.Value;

                sb.Append(string.Format(fmt, dynamoQ.GetFieldLabel(condition.Field.Name), ":" + pId));
            }

            if (sb.Length > 0)
            {
                dynamoQ.AddFilter(sb.ToString(), args);
            }

            q.Conditions.RemoveAll(dbConditions.Contains);
        }
    }
}