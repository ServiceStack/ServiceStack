using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using ServiceStack.Text;

namespace ServiceStack.OrmLite
{
    [Obsolete("Use SqlExpression")]
    public class JoinSqlBuilder<TNewPoco, TBasePoco> : ISqlExpression
    {
        private List<Join> joinList = new List<Join>();
        private List<KeyValuePair<string, WhereType>> whereList = new List<KeyValuePair<string, WhereType>>();
        private List<string> columnList = new List<string>();
        private List<KeyValuePair<string, bool>> orderByList = new List<KeyValuePair<string, bool>>();
        private bool isDistinct = false;
        private bool isAggregateUsed = false;

        private int? Rows { get; set; }
        private int? Offset { get; set; }

        private string baseSchema = "";
        private string baseTableName = "";
        private Type basePocoType;
        private IOrmLiteDialectProvider dialectProvider;

        public JoinSqlBuilder(IOrmLiteDialectProvider dialectProvider=null)
        {
            this.dialectProvider = dialectProvider.ThrowIfNull(nameof(dialectProvider));
            basePocoType = typeof(TBasePoco);
            baseSchema = GetSchema(basePocoType);
            baseTableName = basePocoType.GetModelMetadata().ModelName;
        }

        private string Column<T>(string tableName, Expression<Func<T, object>> func, bool withTablePrefix)
        {
            var lst = ColumnList<T>(tableName, func, withTablePrefix);
            if (lst == null || lst.Count != 1)
                throw new Exception("Expression should have only one column");
            return lst[0];
        }

        private List<string> ColumnList<T>(string tableName, Expression<Func<T, object>> func, bool withTablePrefix = true)
        {
            var result = new List<string>();
            if (func == null)
                return result;
            PropertyList<T>(tableName, func.Body, result, withTablePrefix);
            return result;
        }

        private List<string> ColumnList<T>(bool withTablePrefix = true)
        {
            var pocoType = typeof(T);
            var tableName = pocoType.GetModelMetadata().ModelName;
            List<string> result = new List<string>(pocoType.GetModelMetadata().FieldDefinitions.Count);
            foreach (var item in pocoType.GetModelMetadata().FieldDefinitions)
            {
                if (withTablePrefix)
                    result.Add($"{dialectProvider.GetQuotedTableName(tableName)}.{dialectProvider.GetQuotedColumnName(item.FieldName)}");
                else
                    result.Add($"{dialectProvider.GetQuotedColumnName(item.FieldName)}");
            }
            return result;
        }

        private void ProcessUnary<T>(string tableName, UnaryExpression u, List<string> lst, bool withTablePrefix)
        {
            if (u.NodeType == ExpressionType.Convert)
            {
                if (u.Method != null)
                {
                    throw new Exception("Invalid Expression provided");
                }
                PropertyList<T>(tableName, u.Operand, lst, withTablePrefix);
                return;
            }
            throw new Exception("Invalid Expression provided");
        }

        protected void ProcessMemberAccess<T>(string tableName, MemberExpression m, List<string> lst, bool withTablePrefix, string alias = "")
        {
            if (m.Expression != null
                && (m.Expression.NodeType == ExpressionType.Parameter || m.Expression.NodeType == ExpressionType.Convert))
            {
                var pocoType = typeof(T);
                var fieldName = pocoType.GetModelMetadata().FieldDefinitions.First(f => f.Name == m.Member.Name).FieldName;

                alias = string.IsNullOrEmpty(alias) ? string.Empty : string.Format(" AS {0}", dialectProvider.GetQuotedColumnName(alias));

                if (withTablePrefix)
                    lst.Add($"{dialectProvider.GetQuotedTableName(tableName)}.{dialectProvider.GetQuotedColumnName(fieldName)}{alias}");
                else
                    lst.Add($"{dialectProvider.GetQuotedColumnName(fieldName)}{alias}");
                return;
            }
            throw new Exception("Only Members are allowed");
        }

        private void ProcessNew<T>(string tableName, NewExpression nex, List<string> lst, bool withTablePrefix)
        {
            if (nex.Arguments == null || nex.Arguments.Count == 0)
                throw new Exception("Only column list allowed");

            var expressionProperties = nex.Type.AllProperties();
            for (int i = 0; i < nex.Arguments.Count; i++)
            {
                var arg = nex.Arguments[i];
                var alias = expressionProperties[i].Name;

                PropertyList<T>(tableName, arg, lst, withTablePrefix, alias);
            }
            return;
        }

        private void PropertyList<T>(string tableName, Expression exp, List<string> lst, bool withTablePrefix, string alias = "")
        {
            if (exp == null)
                return;

            switch (exp.NodeType)
            {
                case ExpressionType.MemberAccess:
                    ProcessMemberAccess<T>(tableName, exp as MemberExpression, lst, withTablePrefix, alias);
                    return;

                case ExpressionType.Convert:
                    var ue = exp as UnaryExpression;
                    ProcessUnary<T>(tableName, ue, lst, withTablePrefix);
                    return;

                case ExpressionType.New:
                    ProcessNew<T>(tableName, exp as NewExpression, lst, withTablePrefix);
                    return;
            }
            throw new Exception("Only columns are allowed");
        }

        public JoinSqlBuilder<TNewPoco, TBasePoco> Select<T>(Expression<Func<T, object>> selectColumns)
        {
            Type associatedType = this.PreviousAssociatedType(typeof(T), typeof(T));
            if (associatedType == null)
            {
                throw new Exception("Either the source or destination table should be associated ");
            }

            this.columnList.AddRange(ColumnList(associatedType.GetModelMetadata().ModelName, selectColumns));
            return this;
        }

        public JoinSqlBuilder<TNewPoco, TBasePoco> SelectAll<T>()
        {
            Type associatedType = this.PreviousAssociatedType(typeof(T), typeof(T));
            if (associatedType == null)
            {
                throw new Exception("Either the source or destination table should be associated ");
            }
            this.columnList.AddRange(ColumnList<T>());
            return this;
        }

        public JoinSqlBuilder<TNewPoco, TBasePoco> SelectDistinct()
        {
            isDistinct = true;
            return this;
        }

        public JoinSqlBuilder<TNewPoco, TBasePoco> SelectMax<T>(Expression<Func<T, object>> selectColumn)
        {
            return SelectGenericAggregate<T>(selectColumn, "MAX");
        }

        public JoinSqlBuilder<TNewPoco, TBasePoco> SelectMin<T>(Expression<Func<T, object>> selectColumn)
        {
            return SelectGenericAggregate<T>(selectColumn, "MIN");
        }

        public JoinSqlBuilder<TNewPoco, TBasePoco> SelectCount<T>(Expression<Func<T, object>> selectColumn)
        {
            return SelectGenericAggregate<T>(selectColumn, "COUNT");
        }

        public JoinSqlBuilder<TNewPoco, TBasePoco> SelectAverage<T>(Expression<Func<T, object>> selectColumn)
        {
            return SelectGenericAggregate<T>(selectColumn, "AVG");
        }

        public JoinSqlBuilder<TNewPoco, TBasePoco> SelectSum<T>(Expression<Func<T, object>> selectColumn)
        {
            return SelectGenericAggregate<T>(selectColumn, "SUM");
        }

        private JoinSqlBuilder<TNewPoco, TBasePoco> SelectGenericAggregate<T>(Expression<Func<T, object>> selectColumn, string functionName)
        {
            Type associatedType = this.PreviousAssociatedType(typeof(T), typeof(T));
            if (associatedType == null)
            {
                throw new Exception("Either the source or destination table should be associated ");
            }
            isAggregateUsed = true;

            CheckAggregateUsage(true);

            var columns = ColumnList(associatedType.GetModelMetadata().ModelName, selectColumn);
            if ((columns.Count == 0) || (columns.Count > 1))
            {
                throw new Exception("Expression should select only one Column ");
            }
            this.columnList.Add($" {functionName.ToUpper()}({columns[0]}) ");
            return this;
        }

        public JoinSqlBuilder<TNewPoco, TBasePoco> SelectMin()
        {
            isDistinct = true;
            return this;
        }

        public JoinSqlBuilder<TNewPoco, TBasePoco> Where<T>(Expression<Func<T, bool>> where)
        {
            return WhereInternal(WhereType.AND, where);
        }

        public JoinSqlBuilder<TNewPoco, TBasePoco> Or<T>(Expression<Func<T, bool>> where)
        {
            return WhereInternal(WhereType.OR, where);
        }

        public JoinSqlBuilder<TNewPoco, TBasePoco> And<T>(Expression<Func<T, bool>> where)
        {
            return WhereInternal(WhereType.AND, where);
        }

        private JoinSqlBuilder<TNewPoco, TBasePoco> WhereInternal<T>(WhereType whereType, Expression<Func<T, bool>> where)
        {
            Type associatedType = this.PreviousAssociatedType(typeof(T), typeof(T));
            if (associatedType == null)
            {
                throw new Exception("Either the source or destination table should be associated ");
            }
            var ev = dialectProvider.SqlExpression<T>();
            ev.WhereStatementWithoutWhereString = true;
            ev.PrefixFieldWithTableName = true;
            ev.Where(where);
            var str = ev.WhereExpression;
            if (String.IsNullOrEmpty(str) == false)
            {
                this.whereList.Add(new KeyValuePair<string, WhereType>(str, whereType));
            }
            return this;
        }

        private JoinSqlBuilder<TNewPoco, TBasePoco> OrderByInternal<T>(bool byDesc, Expression<Func<T, object>> orderByColumns)
        {
            Type associatedType = this.PreviousAssociatedType(typeof(T), typeof(T));
            if (associatedType == null)
            {
                throw new Exception("Either the source or destination table should be associated ");
            }

            var lst = ColumnList(associatedType.GetModelMetadata().ModelName, orderByColumns);
            foreach (var item in lst)
                orderByList.Add(new KeyValuePair<string, bool>(item, !byDesc));
            return this;
        }

        public JoinSqlBuilder<TNewPoco, TBasePoco> Clear()
        {
            joinList.Clear();
            whereList.Clear();
            columnList.Clear();
            orderByList.Clear();
            return this;
        }

        public JoinSqlBuilder<TNewPoco, TBasePoco> OrderBy<T>(Expression<Func<T, object>> sourceColumn)
        {
            return OrderByInternal(false, sourceColumn);
        }

        public JoinSqlBuilder<TNewPoco, TBasePoco> OrderByDescending<T>(Expression<Func<T, object>> sourceColumn)
        {
            return OrderByInternal(true, sourceColumn);
        }

        public JoinSqlBuilder<TNewPoco, TBasePoco> Join<TSourceTable, TDestinationTable>(Expression<Func<TSourceTable, object>> sourceColumn, Expression<Func<TDestinationTable, object>> destinationColumn, Expression<Func<TSourceTable, object>> sourceTableColumnSelection = null, Expression<Func<TDestinationTable, object>> destinationTableColumnSelection = null, Expression<Func<TSourceTable, bool>> sourceWhere = null, Expression<Func<TDestinationTable, bool>> destinationWhere = null)
        {
            return JoinInternal<Join, TSourceTable, TDestinationTable>(JoinType.INNER, joinList, sourceColumn, destinationColumn, sourceTableColumnSelection, destinationTableColumnSelection, sourceWhere, destinationWhere);
        }

        public JoinSqlBuilder<TNewPoco, TBasePoco> LeftJoin<TSourceTable, TDestinationTable>(Expression<Func<TSourceTable, object>> sourceColumn, Expression<Func<TDestinationTable, object>> destinationColumn, Expression<Func<TSourceTable, object>> sourceTableColumnSelection = null, Expression<Func<TDestinationTable, object>> destinationTableColumnSelection = null, Expression<Func<TSourceTable, bool>> sourceWhere = null, Expression<Func<TDestinationTable, bool>> destinationWhere = null)
        {
            return JoinInternal<Join, TSourceTable, TDestinationTable>(JoinType.LEFTOUTER, joinList, sourceColumn, destinationColumn, sourceTableColumnSelection, destinationTableColumnSelection, sourceWhere, destinationWhere);
        }

        public JoinSqlBuilder<TNewPoco, TBasePoco> RightJoin<TSourceTable, TDestinationTable>(Expression<Func<TSourceTable, object>> sourceColumn, Expression<Func<TDestinationTable, object>> destinationColumn, Expression<Func<TSourceTable, object>> sourceTableColumnSelection = null, Expression<Func<TDestinationTable, object>> destinationTableColumnSelection = null, Expression<Func<TSourceTable, bool>> sourceWhere = null, Expression<Func<TDestinationTable, bool>> destinationWhere = null)
        {
            return JoinInternal<Join, TSourceTable, TDestinationTable>(JoinType.RIGHTOUTER, joinList, sourceColumn, destinationColumn, sourceTableColumnSelection, destinationTableColumnSelection, sourceWhere, destinationWhere);
        }

        public JoinSqlBuilder<TNewPoco, TBasePoco> FullJoin<TSourceTable, TDestinationTable>(Expression<Func<TSourceTable, object>> sourceColumn, Expression<Func<TDestinationTable, object>> destinationColumn, Expression<Func<TSourceTable, object>> sourceTableColumnSelection = null, Expression<Func<TDestinationTable, object>> destinationTableColumnSelection = null, Expression<Func<TSourceTable, bool>> sourceWhere = null, Expression<Func<TDestinationTable, bool>> destinationWhere = null)
        {
            return JoinInternal<Join, TSourceTable, TDestinationTable>(JoinType.FULLOUTER, joinList, sourceColumn, destinationColumn, sourceTableColumnSelection, destinationTableColumnSelection, sourceWhere, destinationWhere);
        }

        public JoinSqlBuilder<TNewPoco, TBasePoco> CrossJoin<TSourceTable, TDestinationTable>(Expression<Func<TSourceTable, object>> sourceTableColumnSelection = null, Expression<Func<TDestinationTable, object>> destinationTableColumnSelection = null, Expression<Func<TSourceTable, bool>> sourceWhere = null, Expression<Func<TDestinationTable, bool>> destinationWhere = null)
        {
            return JoinInternal<Join, TSourceTable, TDestinationTable>(JoinType.CROSS, joinList, null, null, sourceTableColumnSelection, destinationTableColumnSelection, sourceWhere, destinationWhere);
        }

        private JoinSqlBuilder<TNewPoco, TBasePoco> JoinInternal<TJoin, TSourceTable, TDestinationTable>(JoinType joinType, List<TJoin> joinObjList, Expression<Func<TSourceTable, object>> sourceColumn, Expression<Func<TDestinationTable, object>> destinationColumn, Expression<Func<TSourceTable, object>> sourceTableColumnSelection, Expression<Func<TDestinationTable, object>> destinationTableColumnSelection, Expression<Func<TSourceTable, bool>> sourceWhere = null, Expression<Func<TDestinationTable, bool>> destinationWhere = null) where TJoin : Join, new()
        {
            Type associatedType = this.PreviousAssociatedType(typeof(TSourceTable), typeof(TDestinationTable));
            if (associatedType == null)
            {
                throw new Exception("Either the source or destination table should be associated ");
            }

            TJoin join = new TJoin();
            join.JoinType = joinType;
            join.Class1Type = typeof(TSourceTable);
            join.Class2Type = typeof(TDestinationTable);

            if (associatedType == join.Class1Type)
                join.RefType = join.Class2Type;
            else
                join.RefType = join.Class1Type;

            join.Class1Schema = GetSchema(join.Class1Type);
            join.Class1TableName = join.Class1Type.GetModelMetadata().ModelName;
            join.Class2Schema = GetSchema(join.Class2Type);
            join.Class2TableName = join.Class2Type.GetModelMetadata().ModelName;
            join.RefTypeSchema = GetSchema(join.RefType);
            join.RefTypeTableName = join.RefType.GetModelMetadata().ModelName;

            if (join.JoinType != JoinType.CROSS)
            {
                if (join.JoinType == JoinType.SELF)
                {
                    join.Class1ColumnName = Column<TSourceTable>(join.Class1TableName, sourceColumn, false);
                    join.Class2ColumnName = Column<TDestinationTable>(join.Class2TableName, destinationColumn, false);
                }
                else
                {
                    join.Class1ColumnName = Column<TSourceTable>(join.Class1TableName, sourceColumn, true);
                    join.Class2ColumnName = Column<TDestinationTable>(join.Class2TableName, destinationColumn, true);
                }
            }

            if (sourceTableColumnSelection != null)
            {
                columnList.AddRange(ColumnList<TSourceTable>(join.Class1TableName, sourceTableColumnSelection));
            }

            if (destinationTableColumnSelection != null)
            {
                columnList.AddRange(ColumnList<TDestinationTable>(join.Class2TableName, destinationTableColumnSelection));
            }

            if (sourceWhere != null)
            {
                var ev = dialectProvider.SqlExpression<TSourceTable>();
                ev.WhereStatementWithoutWhereString = true;
                ev.PrefixFieldWithTableName = true;
                ev.Where(sourceWhere);
                var where = ev.WhereExpression;
                if (!String.IsNullOrEmpty(where))
                    whereList.Add(new KeyValuePair<string, WhereType>(where, WhereType.AND));
            }

            if (destinationWhere != null)
            {
                var ev = dialectProvider.SqlExpression<TDestinationTable>();
                ev.WhereStatementWithoutWhereString = true;
                ev.PrefixFieldWithTableName = true;
                ev.Where(destinationWhere);
                var where = ev.WhereExpression;
                if (!String.IsNullOrEmpty(where))
                    whereList.Add(new KeyValuePair<string, WhereType>(where, WhereType.AND));
            }

            joinObjList.Add(join);
            return this;
        }

        private string GetSchema(Type type)
        {
            return string.IsNullOrEmpty(type.GetModelMetadata().Schema) ? string.Empty : string.Format("\"{0}\".", type.GetModelMetadata().Schema);
        }

        private Type PreviousAssociatedType(Type sourceTableType, Type destinationTableType)
        {
            if (sourceTableType == basePocoType || destinationTableType == basePocoType)
            {
                return basePocoType;
            }

            foreach (var j in joinList)
            {
                if (j.Class1Type == sourceTableType || j.Class2Type == sourceTableType)
                {
                    return sourceTableType;
                }
                if (j.Class1Type == destinationTableType || j.Class2Type == destinationTableType)
                {
                    return destinationTableType;
                }
            }
            return null;
        }

        private void CheckAggregateUsage(bool ignoreCurrentItem)
        {
            if ((columnList.Count > (ignoreCurrentItem ? 0 : 1)) && isAggregateUsed)
            {
                throw new Exception("Aggregate function cannot be used with non aggregate select columns");
            }
        }

        /// <summary>
        /// Offset of the first row to return. The offset of the initial row is 0
        /// </summary>
        public virtual JoinSqlBuilder<TNewPoco, TBasePoco> Skip(int? skip = null)
        {
            Offset = skip;
            return this;
        }

        /// <summary>
        /// Number of rows returned by a SELECT statement
        /// </summary>
        public virtual JoinSqlBuilder<TNewPoco, TBasePoco> Take(int? take = null)
        {
            Rows = take;
            return this;
        }

        /// <summary>
        /// Set the specified offset and rows for SQL Limit clause.
        /// </summary>
        /// <param name='skip'>
        /// Offset of the first row to return. The offset of the initial row is 0
        /// </param>
        /// <param name='rows'>
        /// Number of rows returned by a SELECT statement
        /// </param>	
        public virtual JoinSqlBuilder<TNewPoco, TBasePoco> Limit(int skip, int rows)
        {
            Offset = skip;
            Rows = rows;
            return this;
        }

        /// <summary>
        /// Set the specified offset and rows for SQL Limit clause where they exist.
        /// </summary>
        /// <param name='skip'>
        /// Offset of the first row to return. The offset of the initial row is 0
        /// </param>
        /// <param name='rows'>
        /// Number of rows returned by a SELECT statement
        /// </param>	
        public virtual JoinSqlBuilder<TNewPoco, TBasePoco> Limit(int? skip, int? rows)
        {
            Offset = skip;
            Rows = rows;
            return this;
        }

        /// <summary>
        /// Set the specified rows for Sql Limit clause.
        /// </summary>
        /// <param name='rows'>
        /// Number of rows returned by a SELECT statement
        /// </param>
        public virtual JoinSqlBuilder<TNewPoco, TBasePoco> Limit(int rows)
        {
            Offset = null;
            Rows = rows;
            return this;
        }

        /// <summary>
        /// Clear Sql Limit clause
        /// </summary>
        public virtual JoinSqlBuilder<TNewPoco, TBasePoco> Limit()
        {
            Offset = null;
            Rows = null;
            return this;
        }

        public string SelectInto<T>() => SelectInto<T>(QueryType.Select);
        public string SelectInto<T>(QueryType queryType)
        {
            var modelDef = typeof(T).GetModelMetadata();

            CheckAggregateUsage(false);

            var sbSelect = StringBuilderCache.Allocate();
            sbSelect.Append("SELECT ");

            var dbColumns = StringBuilderCache.Allocate();

            if (columnList.Count > 0)
            {
                if (isDistinct)
                    sbSelect.Append(" DISTINCT ");

                foreach (var col in columnList)
                {
                    dbColumns.AppendFormat("{0}{1}", dbColumns.Length > 0 ? "," : "", col);
                }
            }
            else
            {
                // improve performance avoiding multiple calls to GetModelDefinition()
                if (isDistinct && modelDef.FieldDefinitions.Count > 0)
                    sbSelect.Append(" DISTINCT ");

                foreach (var fieldDef in modelDef.FieldDefinitions)
                {
                    dbColumns.AppendFormat("{0}{1}", dbColumns.Length > 0 ? "," : "", 
                        (string.IsNullOrEmpty(fieldDef.BelongToModelName) 
                            ? dialectProvider.GetQuotedTableName(modelDef.ModelName) 
                            : dialectProvider.GetQuotedTableName(fieldDef.BelongToModelName))
                        + "." + dialectProvider.GetQuotedColumnName(fieldDef.FieldName));
                }
                if (dbColumns.Length == 0)
                    dbColumns.AppendFormat("\"{0}{1}\".*", baseSchema, dialectProvider.GetQuotedTableName(baseTableName));
            }

            sbSelect.Append(StringBuilderCache.ReturnAndFree(dbColumns) + " \n");

            var sbBody = StringBuilderCacheAlt.Allocate();
            sbBody.AppendFormat("FROM {0}{1} \n", baseSchema, dialectProvider.GetQuotedTableName(baseTableName));
            int i = 0;
            foreach (var join in joinList)
            {
                i++;
                if (join.JoinType == JoinType.INNER || join.JoinType == JoinType.SELF)
                    sbBody.Append(" INNER JOIN ");
                else if (join.JoinType == JoinType.LEFTOUTER)
                    sbBody.Append(" LEFT OUTER JOIN ");
                else if (join.JoinType == JoinType.RIGHTOUTER)
                    sbBody.Append(" RIGHT OUTER JOIN ");
                else if (join.JoinType == JoinType.FULLOUTER)
                    sbBody.Append(" FULL OUTER JOIN ");
                else if (join.JoinType == JoinType.CROSS)
                {
                    sbBody.Append(" CROSS JOIN ");
                }

                if (join.JoinType == JoinType.CROSS)
                {
                    sbBody.AppendFormat(" {0}{1} ON {2} = {3}  \n", join.RefTypeSchema, dialectProvider.GetQuotedTableName(join.RefTypeTableName));
                }
                else
                {
                    if (join.JoinType != JoinType.SELF)
                    {
                        sbBody.AppendFormat(" {0}{1} ON {2} = {3}  \n", join.RefTypeSchema, dialectProvider.GetQuotedTableName(join.RefTypeTableName), join.Class1ColumnName, join.Class2ColumnName);
                    }
                    else
                    {
                        sbBody.AppendFormat(" {0}{1} AS {2} ON {2}.{3} = \"{1}\".{4}  \n", join.RefTypeSchema, dialectProvider.GetQuotedTableName(join.RefTypeTableName), dialectProvider.GetQuotedTableName(join.RefTypeTableName) + "_" + i.ToString(), join.Class1ColumnName, join.Class2ColumnName);
                    }
                }
            }

            if (whereList.Count > 0)
            {
                var sbWhere = new StringBuilder();
                foreach (var where in whereList)
                {
                    sbWhere.AppendFormat("{0}{1}", sbWhere.Length > 0
                        ? (where.Value == WhereType.OR ? " OR " : " AND ") : "", where.Key);
                }
                sbBody.Append("WHERE " + sbWhere + " \n");
            }

            var sbOrderBy = new StringBuilder();
            if (orderByList.Count > 0)
            {
                foreach (var ob in orderByList)
                {
                    sbOrderBy.AppendFormat("{0}{1} {2} ", sbOrderBy.Length > 0 ? "," : "", ob.Key, ob.Value ? "ASC" : "DESC");
                }
                sbOrderBy.Insert(0, "ORDER BY ");
                sbOrderBy.Append(" \n");
            }

            var sql = dialectProvider.ToSelectStatement(QueryType.Select, modelDef, StringBuilderCache.ReturnAndFree(sbSelect), StringBuilderCacheAlt.ReturnAndFree(sbBody), sbOrderBy.ToString(), offset: Offset, rows: Rows);

            return sql; 
        }

        public string ToSql()
        {
            return SelectInto<TNewPoco>(QueryType.Select);
        }

        public List<IDbDataParameter> Params { get; private set; }

        public string ToSelectStatement() => ToSelectStatement(QueryType.Select);
        public string ToSelectStatement(QueryType forType)
        {
            return SelectInto<TNewPoco>(forType);
        }
    }

    enum WhereType
    {
        AND,
        OR
    }


    enum JoinType
    {
        INNER,
        LEFTOUTER,
        RIGHTOUTER,
        FULLOUTER,
        CROSS,
        SELF
    }

    class Join
    {
        public Type Class1Type { get; set; }
        public Type Class2Type { get; set; }
        public Type RefType { get; set; }
        public JoinType JoinType { get; set; }
        public string Class1Schema { get; set; }
        public string Class2Schema { get; set; }
        public string Class1TableName { get; set; }
        public string Class2TableName { get; set; }
        public string RefTypeSchema { get; set; }
        public string RefTypeTableName { get; set; }
        public string Class1ColumnName { get; set; }
        public string Class2ColumnName { get; set; }
    }
}
