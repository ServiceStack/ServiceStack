using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using ServiceStack.Text;

namespace ServiceStack.OrmLite
{
    public delegate string JoinFormatDelegate(IOrmLiteDialectProvider dialect, ModelDefinition tableDef, string joinExpr);

    public class TableOptions
    {
        public string Expression { get; set; }
        public string Alias { get; set; }

        internal JoinFormatDelegate JoinFormat;
        internal ModelDefinition ModelDef;
        internal string ParamName;
    }

    public abstract partial class SqlExpression<T> : ISqlExpression
    {
        protected List<ModelDefinition> tableDefs = new();

        public List<ModelDefinition> GetAllTables()
        {
            var allTableDefs = new List<ModelDefinition> { modelDef };
            allTableDefs.AddRange(tableDefs);
            return allTableDefs;
        }

        public SqlExpression<T> AddReferenceTableIfNotExists<Target>()
        {
            var tableDef = typeof(Target).GetModelDefinition();
            if (!tableDefs.Contains(tableDef))
                tableDefs.Add(tableDef);
            return this;
        }

        public SqlExpression<T> CustomJoin<Target>(string joinString)
        {
            AddReferenceTableIfNotExists<Target>();
            return CustomJoin(joinString);
        }

        public bool IsJoinedTable(Type type)
        {
            return tableDefs.FirstOrDefault(x => x.ModelType == type) != null;
        }

        public SqlExpression<T> Join<Target>(Expression<Func<T, Target, bool>> joinExpr = null)
        {
            return InternalJoin("INNER JOIN", joinExpr);
        }

        public SqlExpression<T> Join<Target>(Expression<Func<T, Target, bool>> joinExpr, TableOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            
            if (options.Expression != null)
                throw new ArgumentException("Can't set both Join Expression and TableOptions Expression");
            
            return InternalJoin("INNER JOIN", joinExpr, options);
        }
        
        public SqlExpression<T> Join<Target>(Expression<Func<T, Target, bool>> joinExpr, JoinFormatDelegate joinFormat) => 
            InternalJoin("INNER JOIN", joinExpr, joinFormat ?? throw new ArgumentNullException(nameof(joinFormat)));

        public SqlExpression<T> Join<Source, Target>(Expression<Func<Source, Target, bool>> joinExpr = null)
        {
            return InternalJoin("INNER JOIN", joinExpr);
        }

        public SqlExpression<T> Join<Source, Target>(Expression<Func<Source, Target, bool>> joinExpr, JoinFormatDelegate joinFormat) => 
            InternalJoin("INNER JOIN", joinExpr, joinFormat);

        public SqlExpression<T> Join<Source, Target>(Expression<Func<Source, Target, bool>> joinExpr, TableOptions options) => 
            InternalJoin("INNER JOIN", joinExpr, options);

        public SqlExpression<T> Join(Type sourceType, Type targetType, Expression joinExpr = null)
        {
            return InternalJoin("INNER JOIN", joinExpr, sourceType.GetModelDefinition(), targetType.GetModelDefinition());
        }

        public SqlExpression<T> LeftJoin<Target>(Expression<Func<T, Target, bool>> joinExpr = null)
        {
            return InternalJoin("LEFT JOIN", joinExpr);
        }

        public SqlExpression<T> LeftJoin<Target>(Expression<Func<T, Target, bool>> joinExpr, JoinFormatDelegate joinFormat) => 
            InternalJoin("LEFT JOIN", joinExpr, joinFormat ?? throw new ArgumentNullException(nameof(joinFormat)));

        public SqlExpression<T> LeftJoin<Target>(Expression<Func<T, Target, bool>> joinExpr, TableOptions options) => 
            InternalJoin("LEFT JOIN", joinExpr, options ?? throw new ArgumentNullException(nameof(options)));

        public SqlExpression<T> LeftJoin<Source, Target>(Expression<Func<Source, Target, bool>> joinExpr = null)
        {
            return InternalJoin("LEFT JOIN", joinExpr);
        }

        public SqlExpression<T> LeftJoin<Source, Target>(Expression<Func<Source, Target, bool>> joinExpr, JoinFormatDelegate joinFormat) => 
            InternalJoin("LEFT JOIN", joinExpr, joinFormat);

        public SqlExpression<T> LeftJoin<Source, Target>(Expression<Func<Source, Target, bool>> joinExpr, TableOptions options) => 
            InternalJoin("LEFT JOIN", joinExpr, options);

        public SqlExpression<T> LeftJoin(Type sourceType, Type targetType, Expression joinExpr = null)
        {
            return InternalJoin("LEFT JOIN", joinExpr, sourceType.GetModelDefinition(), targetType.GetModelDefinition());
        }

        public SqlExpression<T> RightJoin<Target>(Expression<Func<T, Target, bool>> joinExpr = null)
        {
            return InternalJoin("RIGHT JOIN", joinExpr);
        }

        public SqlExpression<T> RightJoin<Target>(Expression<Func<T, Target, bool>> joinExpr, JoinFormatDelegate joinFormat) => 
            InternalJoin("RIGHT JOIN", joinExpr, joinFormat ?? throw new ArgumentNullException(nameof(joinFormat)));

        public SqlExpression<T> RightJoin<Target>(Expression<Func<T, Target, bool>> joinExpr, TableOptions options) => 
            InternalJoin("RIGHT JOIN", joinExpr, options ?? throw new ArgumentNullException(nameof(options)));

        public SqlExpression<T> RightJoin<Source, Target>(Expression<Func<Source, Target, bool>> joinExpr = null)
        {
            return InternalJoin("RIGHT JOIN", joinExpr);
        }

        public SqlExpression<T> RightJoin<Source, Target>(Expression<Func<Source, Target, bool>> joinExpr, JoinFormatDelegate joinFormat) => 
            InternalJoin("RIGHT JOIN", joinExpr, joinFormat);

        public SqlExpression<T> RightJoin<Source, Target>(Expression<Func<Source, Target, bool>> joinExpr, TableOptions options) => 
            InternalJoin("RIGHT JOIN", joinExpr, options);

        public SqlExpression<T> FullJoin<Target>(Expression<Func<T, Target, bool>> joinExpr = null)
        {
            return InternalJoin("FULL JOIN", joinExpr);
        }

        public SqlExpression<T> FullJoin<Source, Target>(Expression<Func<Source, Target, bool>> joinExpr = null)
        {
            return InternalJoin("FULL JOIN", joinExpr);
        }

        public SqlExpression<T> CrossJoin<Target>(Expression<Func<T, Target, bool>> joinExpr = null)
        {
            return InternalJoin("CROSS JOIN", joinExpr);
        }

        public SqlExpression<T> CrossJoin<Source, Target>(Expression<Func<Source, Target, bool>> joinExpr = null)
        {
            return InternalJoin("CROSS JOIN", joinExpr);
        }

        protected SqlExpression<T> InternalJoin<Source, Target>(string joinType, Expression<Func<Source, Target, bool>> joinExpr, JoinFormatDelegate joinFormat) =>
            InternalJoin(joinType, joinExpr, joinFormat != null ? new TableOptions { JoinFormat = joinFormat } : null);

        protected SqlExpression<T> InternalJoin<Source, Target>(string joinType, Expression<Func<Source, Target, bool>> joinExpr, TableOptions options = null)
        {
            var sourceDef = typeof(Source).GetModelDefinition();
            var targetDef = typeof(Target).GetModelDefinition();

            return InternalJoin(joinType, joinExpr, sourceDef, targetDef, options);
        }
        
        protected SqlExpression<T> InternalJoin<Source, Target>(string joinType, Expression joinExpr)
        {
            var sourceDef = typeof(Source).GetModelDefinition();
            var targetDef = typeof(Target).GetModelDefinition();

            return InternalJoin(joinType, joinExpr, sourceDef, targetDef);
        }

        public SqlExpression<T> Join<Source, Target, T3>(Expression<Func<Source, Target, T3, bool>> joinExpr) => InternalJoin<Source, Target>("INNER JOIN", joinExpr);
        public SqlExpression<T> LeftJoin<Source, Target, T3>(Expression<Func<Source, Target, T3, bool>> joinExpr) => InternalJoin<Source, Target>("LEFT JOIN", joinExpr);
        public SqlExpression<T> RightJoin<Source, Target, T3>(Expression<Func<Source, Target, T3, bool>> joinExpr) => InternalJoin<Source, Target>("RIGHT JOIN", joinExpr);
        public SqlExpression<T> FullJoin<Source, Target, T3>(Expression<Func<Source, Target, T3, bool>> joinExpr) => InternalJoin<Source, Target>("FULL JOIN", joinExpr);

        public SqlExpression<T> Join<Source, Target, T3, T4>(Expression<Func<Source, Target, T3, T4, bool>> joinExpr) => InternalJoin<Source, Target>("INNER JOIN", joinExpr);
        public SqlExpression<T> LeftJoin<Source, Target, T3, T4>(Expression<Func<Source, Target, T3, T4, bool>> joinExpr) => InternalJoin<Source, Target>("LEFT JOIN", joinExpr);
        public SqlExpression<T> RightJoin<Source, Target, T3, T4>(Expression<Func<Source, Target, T3, T4, bool>> joinExpr) => InternalJoin<Source, Target>("RIGHT JOIN", joinExpr);
        public SqlExpression<T> FullJoin<Source, Target, T3, T4>(Expression<Func<Source, Target, T3, T4, bool>> joinExpr) => InternalJoin<Source, Target>("FULL JOIN", joinExpr);

        private string InternalCreateSqlFromExpression(Expression joinExpr, bool isCrossJoin) 
        {
            return $"{(isCrossJoin ? "WHERE" : "ON")} {VisitJoin(joinExpr)}";
        }

        private string InternalCreateSqlFromDefinitions(ModelDefinition sourceDef, ModelDefinition targetDef, bool isCrossJoin) 
        {
            var parentDef = sourceDef;
            var childDef = targetDef;

            var refField = parentDef.GetRefFieldDefIfExists(childDef);
            if (refField == null) 
            {
                parentDef = targetDef;
                childDef = sourceDef;
                refField = parentDef.GetRefFieldDefIfExists(childDef);
            }

            if (refField == null) 
            {
                if(!isCrossJoin)
                    throw new ArgumentException($"Could not infer relationship between {sourceDef.ModelName} and {targetDef.ModelName}");

                return string.Empty;
            }

            return string.Format("{0}\n({1}.{2} = {3}.{4})",
                isCrossJoin ? "WHERE" : "ON",
                DialectProvider.GetQuotedTableName(parentDef),
                SqlColumn(parentDef.PrimaryKey.FieldName),
                DialectProvider.GetQuotedTableName(childDef),
                SqlColumn(refField.FieldName));
        }

        public SqlExpression<T> CustomJoin(string joinString)
        {
            PrefixFieldWithTableName = true;
            FromExpression += " " + joinString;
            return this;
        }

        private TableOptions joinAlias;

        protected virtual SqlExpression<T> InternalJoin(string joinType, Expression joinExpr, ModelDefinition sourceDef, ModelDefinition targetDef, TableOptions options = null)
        {
            PrefixFieldWithTableName = true;

            Reset();
            
            var joinFormat = options?.JoinFormat;
            if (options?.Alias != null) //Set joinAlias
            {
                options.ParamName = joinExpr is LambdaExpression l && l.Parameters.Count == 2
                    ? l.Parameters[1].Name
                    : null;
                if (options.ParamName != null)
                {
                    joinFormat = null;
                    options.ModelDef = targetDef;
                    joinAlias = options;
                }
            } 
            

            if (!tableDefs.Contains(sourceDef))
                tableDefs.Add(sourceDef);
            if (!tableDefs.Contains(targetDef))
                tableDefs.Add(targetDef);

            var isCrossJoin = "CROSS JOIN" == joinType;

            var sqlExpr = joinExpr != null 
                ? InternalCreateSqlFromExpression(joinExpr, isCrossJoin)
                : InternalCreateSqlFromDefinitions(sourceDef, targetDef, isCrossJoin);

            var joinDef = tableDefs.Contains(targetDef) && !tableDefs.Contains(sourceDef)
                ? sourceDef
                : targetDef;

            FromExpression += joinFormat != null
                ? $" {joinType} {joinFormat(DialectProvider, joinDef, sqlExpr)}"
                : joinAlias != null
                    ? $" {joinType} {SqlTable(joinDef)} {DialectProvider.GetQuotedName(joinAlias.Alias)} {sqlExpr}"
                    : $" {joinType} {SqlTable(joinDef)} {sqlExpr}";


            if (joinAlias != null) //Unset joinAlias
            {
                joinAlias = null;
                if (options != null)
                {
                    options.ParamName = null;
                    options.ModelDef = null;
                }
            }

            return this;
        }

        public string SelectInto<TModel>() => SelectInto<TModel>(QueryType.Select);
        public string SelectInto<TModel>(QueryType queryType)
        {
            if ((CustomSelect && OnlyFields == null) || (typeof(TModel) == typeof(T) && !PrefixFieldWithTableName))
            {
                return ToSelectStatement(queryType);
            }

            useFieldName = true;

            var sbSelect = StringBuilderCache.Allocate();
            var selectDef = modelDef;
            var orderedDefs = tableDefs;

            if (typeof(TModel) != typeof(List<object>) && 
                typeof(TModel) != typeof(Dictionary<string, object>) &&
                typeof(TModel) != typeof(object) && //dynamic
                !typeof(TModel).IsValueTuple())
            {
                selectDef = typeof(TModel).GetModelDefinition();
                if (selectDef != modelDef && tableDefs.Contains(selectDef))
                {
                    orderedDefs = tableDefs.ToList(); //clone
                    orderedDefs.Remove(selectDef);
                    orderedDefs.Insert(0, selectDef);
                }
            }

            foreach (var fieldDef in selectDef.FieldDefinitions)
            {
                var found = false;

                if (fieldDef.BelongToModelName != null)
                {
                    var tableDef = orderedDefs.FirstOrDefault(x => x.Name == fieldDef.BelongToModelName);
                    if (tableDef != null)
                    {
                        var matchingField = FindWeakMatch(tableDef, fieldDef);
                        if (matchingField != null)
                        {
                            if (OnlyFields == null || OnlyFields.Contains(fieldDef.Name))
                            {
                                if (sbSelect.Length > 0)
                                    sbSelect.Append(", ");

                                if (fieldDef.CustomSelect == null)
                                {
                                    if (fieldDef.IsRowVersion)
                                    {
                                        sbSelect.Append(DialectProvider.GetRowVersionSelectColumn(fieldDef, DialectProvider.GetTableName(tableDef.ModelName)));
                                    }
                                    else
                                    {
                                        sbSelect.Append($"{GetQuotedColumnName(tableDef, matchingField.Name)} AS {SqlColumn(fieldDef.Name)}");
                                    }
                                }
                                else
                                {
                                    sbSelect.Append(fieldDef.CustomSelect + " AS " + fieldDef.FieldName);
                                }

                                continue;
                            }
                        }
                    }
                }

                foreach (var tableDef in orderedDefs)
                {
                    foreach (var tableFieldDef in tableDef.FieldDefinitions)
                    {
                        if (tableFieldDef.Name == fieldDef.Name)
                        {
                            if (OnlyFields != null && !OnlyFields.Contains(fieldDef.Name))
                                continue;

                            if (sbSelect.Length > 0)
                                sbSelect.Append(", ");

                            var tableAlias = tableDef == modelDef // Use TableAlias if source modelDef
                                ? TableAlias
                                : null;
                                    
                            if (fieldDef.CustomSelect == null)
                            {
                                if (fieldDef.IsRowVersion)
                                {
                                    sbSelect.Append(DialectProvider.GetRowVersionSelectColumn(fieldDef,
                                        DialectProvider.GetTableName(tableAlias ?? tableDef.ModelName, tableDef.Schema)));
                                }
                                else
                                {
                                    sbSelect.Append(tableAlias == null
                                        ? GetQuotedColumnName(tableDef, tableFieldDef.Name)
                                        : GetQuotedColumnName(tableDef, tableAlias, tableFieldDef.Name));

                                    if (tableFieldDef.RequiresAlias)
                                        sbSelect.Append(" AS ").Append(SqlColumn(fieldDef.Name));
                                }
                            }
                            else
                            {
                                sbSelect.Append(tableFieldDef.CustomSelect).Append(" AS ").Append(tableFieldDef.FieldName);
                            }

                            found = true;
                            break;
                        }
                    }

                    if (found)
                        break;
                }

                if (!found)
                {
                    // Add support for auto mapping `{Table}{Field}` convention
                    foreach (var tableDef in orderedDefs)
                    {
                        var matchingField = FindWeakMatch(tableDef, fieldDef);
                        if (matchingField != null)
                        {
                            if (OnlyFields != null && !OnlyFields.Contains(fieldDef.Name))
                                continue;

                            if (sbSelect.Length > 0)
                                sbSelect.Append(", ");

                            var tableAlias = tableDef == modelDef // Use TableAlias if source modelDef
                                ? TableAlias
                                : null;
                                    
                            sbSelect.Append($"{DialectProvider.GetQuotedColumnName(tableDef, tableAlias, matchingField)} as {SqlColumn(fieldDef.Name)}");
                            
                            break;
                        }
                    }
                }
            }

            var select = StringBuilderCache.ReturnAndFree(sbSelect);

            var columns = select.Length > 0 ? select : "*";
            SelectExpression = "SELECT " + (selectDistinct ? "DISTINCT " : "") + columns;

            return ToSelectStatement(queryType);
        }

        private static FieldDefinition FindWeakMatch(ModelDefinition tableDef, FieldDefinition fieldDef)
        {
            return tableDef.FieldDefinitions
                .FirstOrDefault(x =>
                    string.Compare(tableDef.Name + x.Name, fieldDef.Name, StringComparison.OrdinalIgnoreCase) == 0
                    || string.Compare(tableDef.ModelName + x.FieldName, fieldDef.Name, StringComparison.OrdinalIgnoreCase) == 0);
        }

        public virtual SqlExpression<T> Where<Target>(Expression<Func<Target, bool>> predicate) => AppendToWhere("AND", predicate);

        public virtual SqlExpression<T> Where<Source, Target>(Expression<Func<Source, Target, bool>> predicate) => AppendToWhere("AND", predicate);

        public virtual SqlExpression<T> Where<T1, T2, T3>(Expression<Func<T1, T2, T3, bool>> predicate) => AppendToWhere("AND", predicate);

        public virtual SqlExpression<T> Where<T1, T2, T3, T4>(Expression<Func<T1, T2, T3, T4, bool>> predicate) => AppendToWhere("AND", predicate);

        public virtual SqlExpression<T> Where<T1, T2, T3, T4, T5>(Expression<Func<T1, T2, T3, T4, T5, bool>> predicate) => AppendToWhere("AND", predicate);

        public virtual SqlExpression<T> Where<T1, T2, T3, T4, T5, T6>(Expression<Func<T1, T2, T3, T4, T5, T6, bool>> predicate) => AppendToWhere("AND", predicate);

        public virtual SqlExpression<T> Where<T1, T2, T3, T4, T5, T6, T7>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, bool>> predicate) => AppendToWhere("AND", predicate);

        public virtual SqlExpression<T> Where<T1, T2, T3, T4, T5, T6, T7, T8>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, bool>> predicate) => AppendToWhere("AND", predicate);

        public virtual SqlExpression<T> Where<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, bool>> predicate) => AppendToWhere("AND", predicate);

        public virtual SqlExpression<T> Where<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, bool>> predicate) => AppendToWhere("AND", predicate);

        public virtual SqlExpression<T> Where<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, bool>> predicate) => AppendToWhere("AND", predicate);

        public virtual SqlExpression<T> Where<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, bool>> predicate) => AppendToWhere("AND", predicate);

        public virtual SqlExpression<T> Where<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, bool>> predicate) => AppendToWhere("AND", predicate);

        public virtual SqlExpression<T> Where<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, bool>> predicate) => AppendToWhere("AND", predicate);

        public virtual SqlExpression<T> Where<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, bool>> predicate) => AppendToWhere("AND", predicate);

        public virtual SqlExpression<T> And<Target>(Expression<Func<Target, bool>> predicate) => AppendToWhere("AND", predicate);

        public virtual SqlExpression<T> And<Source, Target>(Expression<Func<Source, Target, bool>> predicate) => AppendToWhere("AND", predicate);

        public virtual SqlExpression<T> And<T1, T2, T3>(Expression<Func<T1, T2, T3, bool>> predicate) => AppendToWhere("AND", predicate);

        public virtual SqlExpression<T> And<T1, T2, T3, T4>(Expression<Func<T1, T2, T3, T4, bool>> predicate) => AppendToWhere("AND", predicate);

        public virtual SqlExpression<T> And<T1, T2, T3, T4, T5>(Expression<Func<T1, T2, T3, T4, T5, bool>> predicate) => AppendToWhere("AND", predicate);

        public virtual SqlExpression<T> And<T1, T2, T3, T4, T5, T6>(Expression<Func<T1, T2, T3, T4, T5, T6, bool>> predicate) => AppendToWhere("AND", predicate);

        public virtual SqlExpression<T> And<T1, T2, T3, T4, T5, T6, T7>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, bool>> predicate) => AppendToWhere("AND", predicate);

        public virtual SqlExpression<T> And<T1, T2, T3, T4, T5, T6, T7, T8>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, bool>> predicate) => AppendToWhere("AND", predicate);

        public virtual SqlExpression<T> And<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, bool>> predicate) => AppendToWhere("AND", predicate);

        public virtual SqlExpression<T> And<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, bool>> predicate) => AppendToWhere("AND", predicate);

        public virtual SqlExpression<T> And<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, bool>> predicate) => AppendToWhere("AND", predicate);

        public virtual SqlExpression<T> And<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, bool>> predicate) => AppendToWhere("AND", predicate);

        public virtual SqlExpression<T> And<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, bool>> predicate) => AppendToWhere("AND", predicate);

        public virtual SqlExpression<T> And<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, bool>> predicate) => AppendToWhere("AND", predicate);

        public virtual SqlExpression<T> And<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, bool>> predicate) => AppendToWhere("AND", predicate);

        public virtual SqlExpression<T> Or<Target>(Expression<Func<Target, bool>> predicate) => AppendToWhere("OR", predicate);

        public virtual SqlExpression<T> Or<Source, Target>(Expression<Func<Source, Target, bool>> predicate) => AppendToWhere("OR", predicate);

        public virtual SqlExpression<T> Or<T1, T2, T3>(Expression<Func<T1, T2, T3, bool>> predicate) => AppendToWhere("OR", predicate);

        public virtual SqlExpression<T> Or<T1, T2, T3, T4>(Expression<Func<T1, T2, T3, T4, bool>> predicate) => AppendToWhere("OR", predicate);

        public virtual SqlExpression<T> Or<T1, T2, T3, T4, T5>(Expression<Func<T1, T2, T3, T4, T5, bool>> predicate) => AppendToWhere("OR", predicate);

        public virtual SqlExpression<T> Or<T1, T2, T3, T4, T5, T6>(Expression<Func<T1, T2, T3, T4, T5, T6, bool>> predicate) => AppendToWhere("OR", predicate);

        public virtual SqlExpression<T> Or<T1, T2, T3, T4, T5, T6, T7>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, bool>> predicate) => AppendToWhere("OR", predicate);

        public virtual SqlExpression<T> Or<T1, T2, T3, T4, T5, T6, T7, T8>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, bool>> predicate) => AppendToWhere("OR", predicate);

        public virtual SqlExpression<T> Or<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, bool>> predicate) => AppendToWhere("OR", predicate);

        public virtual SqlExpression<T> Or<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, bool>> predicate) => AppendToWhere("OR", predicate);

        public virtual SqlExpression<T> Or<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, bool>> predicate) => AppendToWhere("OR", predicate);

        public virtual SqlExpression<T> Or<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, bool>> predicate) => AppendToWhere("OR", predicate);

        public virtual SqlExpression<T> Or<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, bool>> predicate) => AppendToWhere("OR", predicate);

        public virtual SqlExpression<T> Or<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, bool>> predicate) => AppendToWhere("OR", predicate);

        public virtual SqlExpression<T> Or<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, bool>> predicate) => AppendToWhere("OR", predicate);

        public Tuple<ModelDefinition,FieldDefinition> FirstMatchingField(string fieldName)
        {
            foreach (var tableDef in tableDefs)
            {
                var firstField = tableDef.FieldDefinitions.FirstOrDefault(x => 
                    string.Compare(x.Name, fieldName, StringComparison.OrdinalIgnoreCase) == 0
                 || string.Compare(x.FieldName, fieldName, StringComparison.OrdinalIgnoreCase) == 0);

                if (firstField != null)
                {
                    return Tuple.Create(tableDef, firstField);
                }
            }
            //Fallback to fully qualified '{Table}{Field}' property convention
            foreach (var tableDef in tableDefs)
            {
                var firstField = tableDef.FieldDefinitions.FirstOrDefault(x =>
                    string.Compare(tableDef.Name + x.Name, fieldName, StringComparison.OrdinalIgnoreCase) == 0
                 || string.Compare(tableDef.ModelName + x.FieldName, fieldName, StringComparison.OrdinalIgnoreCase) == 0);

                if (firstField != null)
                {
                    return Tuple.Create(tableDef, firstField);
                }
            }
            return null;
        }
    }
}