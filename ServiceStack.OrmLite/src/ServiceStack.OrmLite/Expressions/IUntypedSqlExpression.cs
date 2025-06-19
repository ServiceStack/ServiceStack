using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;

namespace ServiceStack.OrmLite
{
    public interface IHasUntypedSqlExpression
    {
        IUntypedSqlExpression GetUntyped();
    }

    public interface IUntypedSqlExpression : ISqlExpression
    {
        string TableAlias { get; set; }
        bool PrefixFieldWithTableName { get; set; }
        bool WhereStatementWithoutWhereString { get; set; }
        IOrmLiteDialectProvider DialectProvider { get; set; }
        string SelectExpression { get; set; }
        string FromExpression { get; set; }
        string BodyExpression { get; }
        string WhereExpression { get; set; }
        string GroupByExpression { get; set; }
        string HavingExpression { get; set; }
        string OrderByExpression { get; set; }
        int? Rows { get; set; }
        int? Offset { get; set; }
        List<string> UpdateFields { get; set; }
        List<string> InsertFields { get; set; }
        ModelDefinition ModelDef { get; }
        IUntypedSqlExpression Clone();

        IUntypedSqlExpression Select();
        IUntypedSqlExpression Select(string selectExpression);
        IUntypedSqlExpression UnsafeSelect(string rawSelect);

        IUntypedSqlExpression Select<Table1, Table2>(Expression<Func<Table1, Table2, object>> fields);
        IUntypedSqlExpression Select<Table1, Table2, Table3>(Expression<Func<Table1, Table2, Table3, object>> fields);
        IUntypedSqlExpression SelectDistinct<Table1, Table2>(Expression<Func<Table1, Table2, object>> fields);
        IUntypedSqlExpression SelectDistinct<Table1, Table2, Table3>(Expression<Func<Table1, Table2, Table3, object>> fields);
        IUntypedSqlExpression SelectDistinct();
        IUntypedSqlExpression From(string tables);
        IUntypedSqlExpression UnsafeFrom(string rawFrom);
        IUntypedSqlExpression Where();
        IUntypedSqlExpression UnsafeWhere(string rawSql, params object[] filterParams);
        IUntypedSqlExpression Ensure(string sqlFilter, params object[] filterParams);
        IUntypedSqlExpression Where(string sqlFilter, params object[] filterParams);
        IUntypedSqlExpression UnsafeAnd(string rawSql, params object[] filterParams);
        IUntypedSqlExpression And(string sqlFilter, params object[] filterParams);
        IUntypedSqlExpression UnsafeOr(string rawSql, params object[] filterParams);
        IUntypedSqlExpression Or(string sqlFilter, params object[] filterParams);
        IUntypedSqlExpression AddCondition(string condition, string sqlFilter, params object[] filterParams);
        IUntypedSqlExpression GroupBy();
        IUntypedSqlExpression GroupBy(string groupBy);
        IUntypedSqlExpression Having();
        IUntypedSqlExpression Having(string sqlFilter, params object[] filterParams);
        IUntypedSqlExpression OrderBy();
        IUntypedSqlExpression OrderBy(string orderBy);
        ModelDefinition GetModelDefinition(FieldDefinition fieldDef);
        IUntypedSqlExpression OrderByFields(params FieldDefinition[] fields);
        IUntypedSqlExpression OrderByFieldsDescending(params FieldDefinition[] fields);
        IUntypedSqlExpression OrderByFields(params string[] fieldNames);
        IUntypedSqlExpression OrderByFieldsDescending(params string[] fieldNames);
        IUntypedSqlExpression OrderBy<Table>(Expression<Func<Table, object>> keySelector);
        IUntypedSqlExpression ThenBy(string orderBy);
        IUntypedSqlExpression ThenBy<Table>(Expression<Func<Table, object>> keySelector);
        IUntypedSqlExpression OrderByDescending<Table>(Expression<Func<Table, object>> keySelector);
        IUntypedSqlExpression OrderByDescending(string orderBy);
        IUntypedSqlExpression ThenByDescending(string orderBy);
        IUntypedSqlExpression ThenByDescending<Table>(Expression<Func<Table, object>> keySelector);

        IUntypedSqlExpression Skip(int? skip = null);
        IUntypedSqlExpression Take(int? take = null);
        IUntypedSqlExpression Limit(int skip, int rows);
        IUntypedSqlExpression Limit(int? skip, int? rows);
        IUntypedSqlExpression Limit(int rows);
        IUntypedSqlExpression Limit();
        IUntypedSqlExpression ClearLimits();
        IUntypedSqlExpression Update(List<string> updateFields);
        IUntypedSqlExpression Update();
        IUntypedSqlExpression Insert(List<string> insertFields);
        IUntypedSqlExpression Insert();

        IDbDataParameter CreateParam(string name, object value = null, ParameterDirection direction = ParameterDirection.Input, DbType? dbType = null);
        IUntypedSqlExpression Join<Source, Target>(Expression<Func<Source, Target, bool>> joinExpr = null);
        IUntypedSqlExpression Join(Type sourceType, Type targetType, Expression joinExpr = null);
        IUntypedSqlExpression LeftJoin<Source, Target>(Expression<Func<Source, Target, bool>> joinExpr = null);
        IUntypedSqlExpression LeftJoin(Type sourceType, Type targetType, Expression joinExpr = null);
        IUntypedSqlExpression RightJoin<Source, Target>(Expression<Func<Source, Target, bool>> joinExpr = null);
        IUntypedSqlExpression FullJoin<Source, Target>(Expression<Func<Source, Target, bool>> joinExpr = null);
        IUntypedSqlExpression CrossJoin<Source, Target>(Expression<Func<Source, Target, bool>> joinExpr = null);
        IUntypedSqlExpression CustomJoin(string joinString);
        IUntypedSqlExpression Ensure<Target>(Expression<Func<Target, bool>> predicate);
        IUntypedSqlExpression Ensure<Source, Target>(Expression<Func<Source, Target, bool>> predicate);
        IUntypedSqlExpression Where<Target>(Expression<Func<Target, bool>> predicate);
        IUntypedSqlExpression Where<Source, Target>(Expression<Func<Source, Target, bool>> predicate);
        IUntypedSqlExpression And<Target>(Expression<Func<Target, bool>> predicate);
        IUntypedSqlExpression And<Source, Target>(Expression<Func<Source, Target, bool>> predicate);
        IUntypedSqlExpression Or<Target>(Expression<Func<Target, bool>> predicate);
        IUntypedSqlExpression Or<Source, Target>(Expression<Func<Source, Target, bool>> predicate);

        string SqlTable(ModelDefinition modelDef);
        string SqlColumn(string columnName);
        string ToDeleteRowStatement();
        string ToCountStatement();
        IList<string> GetAllFields();
        Tuple<ModelDefinition, FieldDefinition> FirstMatchingField(string fieldName);
    }

    public class UntypedSqlExpressionProxy<T> : IUntypedSqlExpression
    {
        private SqlExpression<T> q;
        public UntypedSqlExpressionProxy(SqlExpression<T> q)
        {
            this.q = q;
        }

        public string TableAlias
        {
            get => q.TableAlias;
            set => q.TableAlias = value;
        }

        public bool PrefixFieldWithTableName
        {
            get => q.PrefixFieldWithTableName;
            set => q.PrefixFieldWithTableName = value;
        }

        public bool WhereStatementWithoutWhereString
        {
            get => q.WhereStatementWithoutWhereString;
            set => q.WhereStatementWithoutWhereString = value;
        }

        public IOrmLiteDialectProvider DialectProvider
        {
            get => q.DialectProvider;
            set => q.DialectProvider  = value;
        }

        public List<IDbDataParameter> Params
        {
            get => q.Params;
            set => q.Params = value;
        }

        public string SelectExpression
        {
            get => q.SelectExpression;
            set => q.SelectExpression  = value;
        }

        public string FromExpression
        {
            get => q.FromExpression;
            set => q.FromExpression = value;
        }

        public string BodyExpression => q.BodyExpression;

        public string WhereExpression
        {
            get => q.WhereExpression;
            set => q.WhereExpression = value;
        }

        public string GroupByExpression
        {
            get => q.GroupByExpression;
            set => q.GroupByExpression = value;
        }

        public string HavingExpression
        {
            get => q.HavingExpression;
            set => q.HavingExpression = value;
        }

        public string OrderByExpression
        {
            get => q.OrderByExpression;
            set => q.OrderByExpression = value;
        }

        public int? Rows
        {
            get => q.Rows;
            set => q.Rows = value;
        }

        public int? Offset
        {
            get => q.Offset;
            set => q.Offset = value;
        }

        public List<string> UpdateFields
        {
            get => q.UpdateFields;
            set => q.UpdateFields = value;
        }

        public List<string> InsertFields
        {
            get => q.InsertFields;
            set => q.InsertFields = value;
        }

        public ModelDefinition ModelDef => q.ModelDef;


        public IUntypedSqlExpression Clone()
        {
            q.Clone();
            return this;
        }

        public IUntypedSqlExpression Select()
        {
            q.Select();
            return this;
        }

        public IUntypedSqlExpression Select(string selectExpression)
        {
            q.Select(selectExpression);
            return this;
        }

        public IUntypedSqlExpression UnsafeSelect(string rawSelect)
        {
            q.UnsafeSelect(rawSelect);
            return this;
        }

        public IUntypedSqlExpression Select<Table1, Table2>(Expression<Func<Table1, Table2, object>> fields)
        {
            q.Select(fields);
            return this;
        }

        public IUntypedSqlExpression Select<Table1, Table2, Table3>(Expression<Func<Table1, Table2, Table3, object>> fields)
        {
            q.Select(fields);
            return this;
        }

        public IUntypedSqlExpression SelectDistinct<Table1, Table2>(Expression<Func<Table1, Table2, object>> fields)
        {
            q.SelectDistinct(fields);
            return this;
        }

        public IUntypedSqlExpression SelectDistinct<Table1, Table2, Table3>(Expression<Func<Table1, Table2, Table3, object>> fields)
        {
            q.SelectDistinct(fields);
            return this;
        }

        public IUntypedSqlExpression SelectDistinct()
        {
            q.SelectDistinct();
            return this;
        }

        public IUntypedSqlExpression From(string tables)
        {
            q.From(tables);
            return this;
        }

        public IUntypedSqlExpression UnsafeFrom(string rawFrom)
        {
            q.UnsafeFrom(rawFrom);
            return this;
        }

        public IUntypedSqlExpression Where()
        {
            q.Where();
            return this;
        }

        public IUntypedSqlExpression UnsafeWhere(string rawSql, params object[] filterParams)
        {
            q.UnsafeWhere(rawSql, filterParams);
            return this;
        }

        public IUntypedSqlExpression Ensure(string sqlFilter, params object[] filterParams)
        {
            q.Ensure(sqlFilter, filterParams);
            return this;
        }

        public IUntypedSqlExpression Where(string sqlFilter, params object[] filterParams)
        {
            q.Where(sqlFilter, filterParams);
            return this;
        }

        public IUntypedSqlExpression UnsafeAnd(string rawSql, params object[] filterParams)
        {
            q.UnsafeAnd(rawSql, filterParams);
            return this;
        }

        public IUntypedSqlExpression And(string sqlFilter, params object[] filterParams)
        {
            q.And(sqlFilter, filterParams);
            return this;
        }

        public IUntypedSqlExpression UnsafeOr(string rawSql, params object[] filterParams)
        {
            q.UnsafeOr(rawSql, filterParams);
            return this;
        }

        public IUntypedSqlExpression Or(string sqlFilter, params object[] filterParams)
        {
            q.Or(sqlFilter, filterParams);
            return this;
        }

        public IUntypedSqlExpression AddCondition(string condition, string sqlFilter, params object[] filterParams)
        {
            q.AddCondition(condition, sqlFilter, filterParams);
            return this;
        }

        public IUntypedSqlExpression GroupBy()
        {
            q.GroupBy();
            return this;
        }

        public IUntypedSqlExpression GroupBy(string groupBy)
        {
            q.GroupBy(groupBy);
            return this;
        }

        public IUntypedSqlExpression Having()
        {
            q.Having();
            return this;
        }

        public IUntypedSqlExpression Having(string sqlFilter, params object[] filterParams)
        {
            q.Having(sqlFilter, filterParams);
            return this;
        }

        public IUntypedSqlExpression OrderBy()
        {
            q.OrderBy();
            return this;
        }

        public IUntypedSqlExpression OrderBy(string orderBy)
        {
            q.OrderBy(orderBy);
            return this;
        }

        public ModelDefinition GetModelDefinition(FieldDefinition fieldDef)
        {
            return q.GetModelDefinition(fieldDef);
        }

        public IUntypedSqlExpression OrderByFields(params FieldDefinition[] fields)
        {
            q.OrderByFields(fields);
            return this;
        }

        public IUntypedSqlExpression OrderByFieldsDescending(params FieldDefinition[] fields)
        {
            q.OrderByFieldsDescending(fields);
            return this;
        }

        public IUntypedSqlExpression OrderByFields(params string[] fieldNames)
        {
            q.OrderByFields(fieldNames);
            return this;
        }

        public IUntypedSqlExpression OrderByFieldsDescending(params string[] fieldNames)
        {
            q.OrderByFieldsDescending(fieldNames);
            return this;
        }

        public IUntypedSqlExpression OrderBy<Table>(Expression<Func<Table, object>> keySelector)
        {
            q.OrderBy(keySelector);
            return this;
        }

        public IUntypedSqlExpression ThenBy(string orderBy)
        {
            q.ThenBy(orderBy);
            return this;
        }

        public IUntypedSqlExpression ThenBy<Table>(Expression<Func<Table, object>> keySelector)
        {
            q.ThenBy(keySelector);
            return this;
        }

        public IUntypedSqlExpression OrderByDescending<Table>(Expression<Func<Table, object>> keySelector)
        {
            q.OrderByDescending(keySelector);
            return this;
        }

        public IUntypedSqlExpression OrderByDescending(string orderBy)
        {
            q.OrderByDescending(orderBy);
            return this;
        }

        public IUntypedSqlExpression ThenByDescending(string orderBy)
        {
            q.ThenByDescending(orderBy);
            return this;
        }

        public IUntypedSqlExpression ThenByDescending<Table>(Expression<Func<Table, object>> keySelector)
        {
            q.ThenByDescending(keySelector);
            return this;
        }

        public IUntypedSqlExpression Skip(int? skip = null)
        {
            q.Skip(skip);
            return this;
        }

        public IUntypedSqlExpression Take(int? take = null)
        {
            q.Take(take);
            return this;
        }

        public IUntypedSqlExpression Limit(int skip, int rows)
        {
            q.Limit(skip, rows);
            return this;
        }

        public IUntypedSqlExpression Limit(int? skip, int? rows)
        {
            q.Limit(skip, rows);
            return this;
        }

        public IUntypedSqlExpression Limit(int rows)
        {
            q.Limit(rows);
            return this;
        }

        public IUntypedSqlExpression Limit()
        {
            q.Limit();
            return this;
        }

        public IUntypedSqlExpression ClearLimits()
        {
            q.ClearLimits();
            return this;
        }

        public IUntypedSqlExpression Update(List<string> updateFields)
        {
            q.Update(updateFields);
            return this;
        }

        public IUntypedSqlExpression Update()
        {
            q.Update();
            return this;
        }

        public IUntypedSqlExpression Insert(List<string> insertFields)
        {
            q.Insert(insertFields);
            return this;
        }

        public IUntypedSqlExpression Insert()
        {
            q.Insert();
            return this;
        }

        public IDbDataParameter CreateParam(string name, object value = null, ParameterDirection direction = ParameterDirection.Input, DbType? dbType = null)
        {
            return q.CreateParam(name, value, direction, dbType);
        }

        public IUntypedSqlExpression Join<Source, Target>(Expression<Func<Source, Target, bool>> joinExpr = null)
        {
            q.Join(joinExpr);
            return this;
        }

        public IUntypedSqlExpression Join(Type sourceType, Type targetType, Expression joinExpr = null)
        {
            q.Join(sourceType, targetType, joinExpr);
            return this;
        }

        public IUntypedSqlExpression LeftJoin<Source, Target>(Expression<Func<Source, Target, bool>> joinExpr = null)
        {
            q.LeftJoin(joinExpr);
            return this;
        }

        public IUntypedSqlExpression LeftJoin(Type sourceType, Type targetType, Expression joinExpr = null)
        {
            q.LeftJoin(sourceType, targetType, joinExpr);
            return this;
        }

        public IUntypedSqlExpression RightJoin<Source, Target>(Expression<Func<Source, Target, bool>> joinExpr = null)
        {
            q.RightJoin(joinExpr);
            return this;
        }

        public IUntypedSqlExpression FullJoin<Source, Target>(Expression<Func<Source, Target, bool>> joinExpr = null)
        {
            q.FullJoin(joinExpr);
            return this;
        }

        public IUntypedSqlExpression CrossJoin<Source, Target>(Expression<Func<Source, Target, bool>> joinExpr = null)
        {
            q.CrossJoin(joinExpr);
            return this;
        }

        public IUntypedSqlExpression CustomJoin(string joinString)
        {
            q.CustomJoin(joinString);
            return this;
        }

        public IUntypedSqlExpression Where<Target>(Expression<Func<Target, bool>> predicate)
        {
            q.Where(predicate);
            return this;
        }

        public IUntypedSqlExpression Ensure<Target>(Expression<Func<Target, bool>> predicate)
        {
            q.Ensure(predicate);
            return this;
        }

        public IUntypedSqlExpression Where<Source, Target>(Expression<Func<Source, Target, bool>> predicate)
        {
            q.Where(predicate);
            return this;
        }

        public IUntypedSqlExpression Ensure<Source, Target>(Expression<Func<Source, Target, bool>> predicate)
        {
            q.Ensure(predicate);
            return this;
        }

        public IUntypedSqlExpression And<Target>(Expression<Func<Target, bool>> predicate)
        {
            q.And(predicate);
            return this;
        }

        public IUntypedSqlExpression And<Source, Target>(Expression<Func<Source, Target, bool>> predicate)
        {
            q.And(predicate);
            return this;
        }

        public IUntypedSqlExpression Or<Target>(Expression<Func<Target, bool>> predicate)
        {
            q.Or(predicate);
            return this;
        }

        public IUntypedSqlExpression Or<Source, Target>(Expression<Func<Source, Target, bool>> predicate)
        {
            q.Or(predicate);
            return this;
        }

        public string SqlTable(ModelDefinition modelDef)
        {
            return q.SqlTable(modelDef);
        }

        public string SqlColumn(string columnName)
        {
            return q.SqlColumn(columnName);
        }

        public string ToDeleteRowStatement()
        {
            return q.ToDeleteRowStatement();
        }

        public string ToSelectStatement() => ToSelectStatement(QueryType.Select);
        public string ToSelectStatement(QueryType forType)
        {
            return q.ToSelectStatement(forType);
        }

        public string ToCountStatement()
        {
            return q.ToCountStatement();
        }

        public IList<string> GetAllFields()
        {
            return q.GetAllFields();
        }

        public Tuple<ModelDefinition, FieldDefinition> FirstMatchingField(string fieldName)
        {
            return q.FirstMatchingField(fieldName);
        }

        public string SelectInto<TModel>() => q.SelectInto<TModel>();
        public string SelectInto<TModel>(QueryType queryType) => q.SelectInto<TModel>(queryType);
    }

    public static class SqlExpressionExtensions
    {
        public static IUntypedSqlExpression GetUntypedSqlExpression(this ISqlExpression sqlExpression)
        {
            var hasUntyped = sqlExpression as IHasUntypedSqlExpression;
            return hasUntyped?.GetUntyped();
        }

        public static IOrmLiteDialectProvider ToDialectProvider(this ISqlExpression sqlExpression) =>
            (sqlExpression as IHasDialectProvider)?.DialectProvider ?? OrmLiteConfig.DialectProvider;

        public static string Table<T>(this ISqlExpression sqlExpression) => sqlExpression.ToDialectProvider().GetQuotedTableName(typeof(T).GetModelDefinition());

        public static string Table<T>(this IOrmLiteDialectProvider dialect) => dialect.GetQuotedTableName(typeof(T).GetModelDefinition());

        public static string Column<Table>(this ISqlExpression sqlExpression, Expression<Func<Table, object>> propertyExpression, bool prefixTable = false) => 
            sqlExpression.ToDialectProvider().Column(propertyExpression, prefixTable);

        public static string Column<Table>(this IOrmLiteDialectProvider dialect, Expression<Func<Table, object>> propertyExpression, bool prefixTable = false)
        {
            string propertyName = null;
            Expression expr = propertyExpression;

            if (expr is LambdaExpression lambda)
                expr = lambda.Body;

            if (expr.NodeType == ExpressionType.Convert && expr is UnaryExpression unary)
                expr = unary.Operand;

            if (expr is MemberExpression member)
                propertyName = member.Member.Name;

            if (propertyName == null)
                propertyName = expr.ToPropertyInfo()?.Name;

            if (propertyName != null)
                return dialect.Column<Table>(propertyName, prefixTable);

            throw new ArgumentException("Expected Lambda MemberExpression but received: " + propertyExpression.Name);
        }

        public static string Column<Table>(this ISqlExpression sqlExpression, string propertyName, bool prefixTable = false) =>
            sqlExpression.ToDialectProvider().Column<Table>(propertyName, prefixTable);

        public static string Column<Table>(this IOrmLiteDialectProvider dialect, string propertyName, bool prefixTable = false)
        {
            var tableDef = typeof(Table).GetModelDefinition();

            var fieldDef = tableDef.FieldDefinitions.FirstOrDefault(x => x.Name == propertyName);
            var fieldName = fieldDef != null
                ? fieldDef.FieldName
                : propertyName;

            return prefixTable
                ? fieldDef != null 
                    ? dialect.GetQuotedColumnName(tableDef, fieldDef) 
                    : dialect.GetQuotedColumnName(tableDef, fieldName)
                : dialect.GetQuotedColumnName(fieldDef);
        }
    }
}