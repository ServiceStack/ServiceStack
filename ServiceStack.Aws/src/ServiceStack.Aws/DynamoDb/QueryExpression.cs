// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using ServiceStack.Text;

namespace ServiceStack.Aws.DynamoDb
{
    public class QueryExpression : QueryRequest, IDynamoCommonQuery
    {
        protected IPocoDynamo Db { get; set; }

        protected DynamoMetadataType Table { get; set; }

        public QueryExpression Projection<TModel>()
        {
            this.SelectFields(typeof(TModel).AllFields().Where(Table.HasField));
            return this;
        }

        public void AddArguments(Dictionary<string, object> args)
        {
            if (args != null)
            {
                Db.ToExpressionAttributeValues(args).Each(x =>
                    ExpressionAttributeValues[x.Key] = x.Value);
            }
        }
    }

    public class QueryExpression<T> : QueryExpression
    {
        public QueryExpression(IPocoDynamo db)
            : this(db, db.GetTableMetadata(typeof(T))) {}

        public QueryExpression(IPocoDynamo db, DynamoMetadataType table)
        {
            this.Db = db;
            this.Table = table;
            this.TableName = this.Table.Name;
        }

        public QueryExpression<T> Clone()
        {
            var q = new QueryExpression<T>(Db)
            {
                Table = Table,
                TableName = TableName,
                AttributesToGet = new List<string>(AttributesToGet),
                ConditionalOperator = ConditionalOperator,
                ConsistentRead = ConsistentRead,
                ExclusiveStartKey = new Dictionary<string, AttributeValue>(ExclusiveStartKey),
                ExpressionAttributeNames = new Dictionary<string, string>(ExpressionAttributeNames),
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>(ExpressionAttributeValues),
                FilterExpression = FilterExpression,
                IndexName = IndexName,
                KeyConditionExpression = KeyConditionExpression,
                KeyConditions = new Dictionary<string, Condition>(KeyConditions),
                Limit = Limit,
                ProjectionExpression = ProjectionExpression,
                QueryFilter = new Dictionary<string, Condition>(QueryFilter),
                ReturnConsumedCapacity = ReturnConsumedCapacity,
                ScanIndexForward = ScanIndexForward,                                
            }.SetSelect(base.Select);

#if !NETCORE
            if (ReadWriteTimeoutInternal != null)
                q.ReadWriteTimeoutInternal = ReadWriteTimeoutInternal;
            if (TimeoutInternal != null)
                q.TimeoutInternal = TimeoutInternal;
#endif

            return q;
        }

        internal QueryExpression<T> SetSelect(Select select)
        {
            base.Select = select;
            return this;
        }

        public QueryExpression<T> AddKeyCondition(string keyCondition)
        {
            if (this.KeyConditionExpression == null)
                this.KeyConditionExpression = keyCondition;
            else
                this.KeyConditionExpression += " AND " + keyCondition;

            return this;
        }

        public QueryExpression<T> AddFilterExpression(string filterExpression)
        {
            if (this.FilterExpression == null)
                this.FilterExpression = filterExpression;
            else
                this.FilterExpression += " AND " + filterExpression;

            return this;
        }

        public QueryExpression<T> KeyCondition(string filterExpression, Dictionary<string, object> args = null, Dictionary<string, string> aliases = null)
        {
            AddKeyCondition(filterExpression);
            AddExpressionNamesAndValues(args, aliases);
            return this;
        }

        public QueryExpression<T> KeyCondition(string filterExpression, object args, Dictionary<string, string> aliases = null)
        {
            return KeyCondition(filterExpression, args.ToObjectDictionary(), aliases);
        }

        public QueryExpression<T> KeyCondition(Expression<Func<T, bool>> filterExpression)
        {
            var q = PocoDynamoExpression.Create(typeof(T), filterExpression, paramPrefix: "k");
            return KeyCondition(q.FilterExpression, q.Params, q.Aliases);
        }

        public QueryExpression<T> LocalIndex(Expression<Func<T, bool>> keyExpression, string indexName = null)
        {
            var q = PocoDynamoExpression.Create(typeof(T), keyExpression, paramPrefix: "i");

            if (q.ReferencedFields.Distinct().Count() != 1)
                throw new ArgumentException("Only 1 Index can be queried per QueryRequest");

            if (indexName == null)
            {
                var indexField = q.ReferencedFields.First();
                var index = q.Table.GetIndexByField(indexField);

                if (index == null)
                    throw new ArgumentException($"Could not find index for field '{indexField}'");

                this.IndexName = index.Name;
            }
            else
            {
                this.IndexName = indexName;
            }

            AddKeyCondition(q.FilterExpression);
            AddExpressionNamesAndValues(q.Params, q.Aliases);

            return this;
        }

        public void AddFilter(string filterExpression, Dictionary<string, object> args)
        {
            Filter(filterExpression, args);
        }

        public QueryExpression<T> Filter(string filterExpression, Dictionary<string, object> args = null, Dictionary<string,string> aliases = null)
        {
            AddFilterExpression(filterExpression);
            AddExpressionNamesAndValues(args, aliases);
            return this;
        }

        private void AddExpressionNamesAndValues(Dictionary<string, object> args, Dictionary<string, string> aliases)
        {
            if (args != null)
            {
                Db.ToExpressionAttributeValues(args).Each(x =>
                    this.ExpressionAttributeValues[x.Key] = x.Value);
            }

            if (aliases != null)
            {
                foreach (var entry in aliases)
                {
                    this.ExpressionAttributeNames[entry.Key] = entry.Value;
                }
            }
        }

        public QueryExpression<T> Filter(string filterExpression, object args, Dictionary<string, string> aliases = null)
        {
            return Filter(filterExpression, args.ToObjectDictionary());
        }

        public QueryExpression<T> Filter(Expression<Func<T, bool>> filterExpression)
        {
            var q = PocoDynamoExpression.Create(typeof(T), filterExpression, paramPrefix: "p");
            return Filter(q.FilterExpression, q.Params, q.Aliases);
        }

        public QueryExpression<T> OrderByAscending()
        {
            this.ScanIndexForward = true;
            return this;
        }

        public QueryExpression<T> OrderByDescending()
        {
            this.ScanIndexForward = false;
            return this;
        }

        public QueryExpression<T> PagingLimit(int limit)
        {
            this.Limit = limit;
            return this;
        }

        public new QueryExpression<T> Select(IEnumerable<string> fields)
        {
            this.SelectFields(fields);
            return this;
        }

        /// <summary>
        /// Select all table fields, useful when querying an index with only a partial field set
        /// </summary>
        public QueryExpression<T> SelectTableFields()
        {
            return Select(Table.Fields.Map(x => x.Name));
        }

        public new QueryExpression<T> Select<TModel>()
        {
            return Select(typeof(TModel).AllFields().Where(Table.HasField));
        }

        public new QueryExpression<T> Select(Func<T, object> fields)
        {
            return Select(fields(typeof(T).CreateInstance<T>()).GetType().AllFields());
        }

        public new QueryExpression<T> Select<TModel>(Func<T, object> fields)
        {
            return Select(fields(typeof(TModel).CreateInstance<T>()).GetType().AllFields()
                .Where(Table.HasField));
        }

        public IEnumerable<T> Exec() => Db.Query(this);
        public List<T> Exec(int limit) => Db.Query(this, limit:limit);
        public IEnumerable<Into> ExecInto<Into>() => Db.Query<Into>(this.Projection<Into>());
        public List<Into> Exec<Into>(int limit) => Db.Query<Into>(this.Projection<Into>(), limit:limit);

#if NET472 || NETCORE
        public async Task<List<T>> ExecAsync(CancellationToken token=default) => await Db.QueryAsync(this, token).ToListAsync(token);
        public async Task<List<T>> ExecAsync(int limit, CancellationToken token=default) => await Db.QueryAsync(this, limit:limit, token: token).ToListAsync(token);
        public async Task<List<Into>> ExecIntoAsync<Into>(CancellationToken token=default) => await Db.QueryAsync<Into>(this.Projection<Into>(), token).ToListAsync(token);
        public async Task<List<Into>> ExecAsync<Into>(int limit, CancellationToken token=default) => await Db.QueryAsync<Into>(this.Projection<Into>(), limit:limit, token).ToListAsync(token);
#else
        public async Task<List<T>> ExecAsync(CancellationToken token=default) => await Db.QueryAsync(this, token).ConfigAwait();
        public async Task<List<T>> ExecAsync(int limit, CancellationToken token=default) => await Db.QueryAsync(this, limit:limit, token: token).ConfigAwait();
        public async Task<List<Into>> ExecIntoAsync<Into>(CancellationToken token=default) => await Db.QueryAsync<Into>(this.Projection<Into>(), token).ConfigAwait();
        public async Task<List<Into>> ExecAsync<Into>(int limit, CancellationToken token=default) => await Db.QueryAsync<Into>(this.Projection<Into>(), limit:limit, token).ConfigAwait();
#endif

        public IEnumerable<TKey> ExecColumn<TKey>(Expression<Func<T, TKey>> fields)
        {
            var q = new PocoDynamoExpression(typeof(T)).Parse(fields);
            var field = q.ReferencedFields[0];
            this.ProjectionExpression = field;

            foreach (var attrValue in Db.Query(this))
            {
                object value = Table.GetField(field).GetValue(attrValue);
                yield return (TKey)value;
            }
        }

        public async Task<List<TKey>> ExecColumnAsync<TKey>(Expression<Func<T, TKey>> fields, CancellationToken token=default)
        {
            var q = new PocoDynamoExpression(typeof(T)).Parse(fields);
            var field = q.ReferencedFields[0];
            this.ProjectionExpression = field;

#if NET472 || NETCORE
            return await Db.QueryAsync(this, token)
                .Select(attrValue => Table.GetField(field).GetValue(attrValue))
                .Select(value => (TKey) value)
                .ToListAsync(token);
#else
            var to = new List<TKey>();
            foreach (var attrValue in await Db.QueryAsync(this, token).ConfigAwait())
            {
                object value = Table.GetField(field).GetValue(attrValue);
                to.Add((TKey) value);
            }
            return to;
#endif
        }
    }
}