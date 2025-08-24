using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using ServiceStack.Text;

namespace ServiceStack.OrmLite;

[Obsolete("Use SqlExpression")]
public class JoinSqlBuilder<TNewPoco, TBasePoco> : ISqlExpression
{
    private List<Join> joinList = new();
    private List<KeyValuePair<string, WhereType>> whereList = new();
    private List<string> columnList = new();
    private List<KeyValuePair<string, bool>> orderByList = new();
    private bool isDistinct = false;
    private bool isAggregateUsed = false;

    private int? Rows { get; set; }
    private int? Offset { get; set; }

    private ModelDefinition baseDef;
    private Type baseType;
    private IOrmLiteDialectProvider dialectProvider;

    public JoinSqlBuilder(IOrmLiteDialectProvider dialectProvider=null)
    {
        this.dialectProvider = dialectProvider.ThrowIfNull(nameof(dialectProvider));
        baseType = typeof(TBasePoco);
        baseDef = baseType.GetModelMetadata();
    }

    private string Column<T>(ModelDefinition modelDef, Expression<Func<T, object>> func, bool withTablePrefix)
    {
        var lst = ColumnList<T>(modelDef, func, withTablePrefix);
        if (lst is not { Count: 1 })
            throw new Exception("Expression should have only one column");
        return lst[0];
    }

    private List<string> ColumnList<T>(ModelDefinition modelDef, Expression<Func<T, object>> func, bool withTablePrefix = true)
    {
        var result = new List<string>();
        if (func == null)
            return result;
        PropertyList<T>(modelDef, func.Body, result, withTablePrefix);
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

    private void ProcessUnary<T>(ModelDefinition modelDef, UnaryExpression u, List<string> lst, bool withTablePrefix)
    {
        if (u.NodeType == ExpressionType.Convert)
        {
            if (u.Method != null)
            {
                throw new Exception("Invalid Expression provided");
            }
            PropertyList<T>(modelDef, u.Operand, lst, withTablePrefix);
            return;
        }
        throw new Exception("Invalid Expression provided");
    }

    protected void ProcessMemberAccess<T>(ModelDefinition modelDef, MemberExpression m, List<string> lst, bool withTablePrefix, string alias = "")
    {
        if (m.Expression is { NodeType: ExpressionType.Parameter or ExpressionType.Convert })
        {
            var pocoType = typeof(T);
            var fieldDef = pocoType.GetModelMetadata().FieldDefinitions.First(f => f.Name == m.Member.Name);

            alias = string.IsNullOrEmpty(alias) ? string.Empty : string.Format(" AS {0}", dialectProvider.GetQuotedColumnName(alias));

            if (withTablePrefix)
                lst.Add($"{dialectProvider.GetQuotedTableName(modelDef)}.{dialectProvider.GetQuotedColumnName(fieldDef)}{alias}");
            else
                lst.Add($"{dialectProvider.GetQuotedColumnName(fieldDef)}{alias}");
            return;
        }
        throw new Exception("Only Members are allowed");
    }

    private void ProcessNew<T>(ModelDefinition modelDef, NewExpression nex, List<string> lst, bool withTablePrefix)
    {
        if (nex.Arguments == null || nex.Arguments.Count == 0)
            throw new Exception("Only column list allowed");

        var expressionProperties = nex.Type.AllProperties();
        for (int i = 0; i < nex.Arguments.Count; i++)
        {
            var arg = nex.Arguments[i];
            var alias = expressionProperties[i].Name;

            PropertyList<T>(modelDef, arg, lst, withTablePrefix, alias);
        }
        return;
    }

    private void PropertyList<T>(ModelDefinition modelDef, Expression exp, List<string> lst, bool withTablePrefix, string alias = "")
    {
        if (exp == null)
            return;

        switch (exp.NodeType)
        {
            case ExpressionType.MemberAccess:
                ProcessMemberAccess<T>(modelDef, exp as MemberExpression, lst, withTablePrefix, alias);
                return;

            case ExpressionType.Convert:
                var ue = exp as UnaryExpression;
                ProcessUnary<T>(modelDef, ue, lst, withTablePrefix);
                return;

            case ExpressionType.New:
                ProcessNew<T>(modelDef, exp as NewExpression, lst, withTablePrefix);
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

        this.columnList.AddRange(ColumnList(associatedType.GetModelMetadata(), selectColumns));
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

        var columns = ColumnList(associatedType.GetModelMetadata(), selectColumn);
        if (columns.Count is 0 or > 1)
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

        var lst = ColumnList(associatedType.GetModelMetadata(), orderByColumns);
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

        TJoin join = new TJoin
        {
            JoinType = joinType,
            Class1 = typeof(TSourceTable).GetModelMetadata(),
            Class2 = typeof(TDestinationTable).GetModelMetadata(),
        };

        var refType = associatedType == join.Class1.ModelType ? join.Class2.ModelType : join.Class1.ModelType;
        join.Ref = refType.GetModelMetadata();

        if (join.JoinType != JoinType.CROSS)
        {
            if (join.JoinType == JoinType.SELF)
            {
                join.Class1ColumnName = Column<TSourceTable>(join.Class1, sourceColumn, false);
                join.Class2ColumnName = Column<TDestinationTable>(join.Class2, destinationColumn, false);
            }
            else
            {
                join.Class1ColumnName = Column<TSourceTable>(join.Class1, sourceColumn, true);
                join.Class2ColumnName = Column<TDestinationTable>(join.Class2, destinationColumn, true);
            }
        }

        if (sourceTableColumnSelection != null)
        {
            columnList.AddRange(ColumnList<TSourceTable>(join.Class1, sourceTableColumnSelection));
        }

        if (destinationTableColumnSelection != null)
        {
            columnList.AddRange(ColumnList<TDestinationTable>(join.Class2, destinationTableColumnSelection));
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

    private string GetSchema(ModelDefinition modelDef)
    {
        return string.IsNullOrEmpty(modelDef.Schema) 
            ? dialectProvider.GetQuotedName(dialectProvider.NamingStrategy.GetSchemaName(modelDef)) 
            : $"\"{modelDef.Schema}\".";
    }

    private Type PreviousAssociatedType(Type sourceTableType, Type destinationTableType)
    {
        if (sourceTableType == baseType || destinationTableType == baseType)
        {
            return baseType;
        }

        foreach (var j in joinList)
        {
            if (j.Class1.ModelType == sourceTableType || j.Class2.ModelType == sourceTableType)
            {
                return sourceTableType;
            }
            if (j.Class1.ModelType == destinationTableType || j.Class2.ModelType == destinationTableType)
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
                        ? dialectProvider.GetQuotedTableName(modelDef) 
                        : dialectProvider.GetQuotedTableName(fieldDef.BelongToModelName))
                    + "." + dialectProvider.GetQuotedColumnName(fieldDef));
            }
            if (dbColumns.Length == 0)
                dbColumns.Append($"{dialectProvider.GetQuotedTableName(baseDef)}.*");
        }

        sbSelect.Append(StringBuilderCache.ReturnAndFree(dbColumns) + " \n");

        var sbBody = StringBuilderCacheAlt.Allocate();
        sbBody.AppendFormat("FROM {0} \n", dialectProvider.GetQuotedTableName(baseDef));
        int i = 0;
        foreach (var join in joinList)
        {
            i++;
            if (join.JoinType is JoinType.INNER or JoinType.SELF)
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
                sbBody.AppendFormat(" {0} ON {1} = {2}  \n", dialectProvider.GetQuotedTableName(join.Ref), join.Class1ColumnName, join.Class2ColumnName);
            }
            else
            {
                if (join.JoinType != JoinType.SELF)
                {
                    sbBody.AppendFormat(" {0} ON {1} = {2}  \n", dialectProvider.GetQuotedTableName(join.Ref), join.Class1ColumnName, join.Class2ColumnName);
                }
                else
                {
                    sbBody.AppendFormat(" {0} AS {1} ON {1}.{2} = {0}.{3}  \n", dialectProvider.GetQuotedTableName(join.Ref), dialectProvider.GetQuotedTableName(join.Ref.ModelName + "_" + i), join.Class1ColumnName, join.Class2ColumnName);
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
    public ModelDefinition Class1 { get; set; }
    public ModelDefinition Class2 { get; set; }
    public ModelDefinition Ref { get; set; }
    public JoinType JoinType { get; set; }
    public string Class1ColumnName { get; set; }
    public string Class2ColumnName { get; set; }
}