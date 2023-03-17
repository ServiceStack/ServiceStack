using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using ServiceStack.Text;

namespace ServiceStack.OrmLite
{
    public abstract partial class SqlExpression<T> : ISqlExpression, IHasUntypedSqlExpression, IHasDialectProvider
    {
        public const string TrueLiteral = "(1=1)";
        public const string FalseLiteral = "(1=0)";

        private Expression<Func<T, bool>> underlyingExpression;
        private List<string> orderByProperties = new();
        private string selectExpression = string.Empty;
        private string fromExpression = null;
        private string whereExpression;
        private string groupBy = string.Empty;
        private string havingExpression;
        private string orderBy = string.Empty;
        public HashSet<string> OnlyFields { get; protected set; }

        public List<string> UpdateFields { get; set; }
        public List<string> InsertFields { get; set; }

        private string sep = string.Empty;
        protected ModelDefinition modelDef;
        public string TableAlias { get; set; }
        public IOrmLiteDialectProvider DialectProvider { get; set; }
        public List<IDbDataParameter> Params { get; set; }
        public Func<string,string> SqlFilter { get; set; }
        public static Action<SqlExpression<T>> SelectFilter { get; set; }
        public int? Rows { get; set; }
        public int? Offset { get; set; }
        public bool PrefixFieldWithTableName { get; set; }
        public bool UseSelectPropertiesAsAliases { get; set; }
        public bool WhereStatementWithoutWhereString { get; set; }
        public ISet<string> Tags { get; } = new HashSet<string>();

        protected bool CustomSelect { get; set; }
        protected bool useFieldName = false;
        protected bool selectDistinct = false;
        protected bool visitedExpressionIsTableColumn = false;
        protected bool skipParameterizationForThisExpression = false;
        protected bool isSelectExpression = false;
        private bool hasEnsureConditions = false;
        private bool inSqlMethodCall = false;

        protected string Sep => sep;

        protected SqlExpression(IOrmLiteDialectProvider dialectProvider)
        {
            UpdateFields = new List<string>();
            InsertFields = new List<string>();

            modelDef = typeof(T).GetModelDefinition();
            PrefixFieldWithTableName = OrmLiteConfig.IncludeTablePrefixes;
            WhereStatementWithoutWhereString = false;

            DialectProvider = dialectProvider;
            Params = new List<IDbDataParameter>();
            tableDefs.Add(modelDef);

            var initFilter = OrmLiteConfig.SqlExpressionInitFilter;
            initFilter?.Invoke(this.GetUntyped());
        }

        public SqlExpression<T> Clone()
        {
            return CopyTo(DialectProvider.SqlExpression<T>());
        }

        public virtual void AddTag(string tag)
        {
            Debug.Assert(!string.IsNullOrEmpty(tag));
            Tags.Add(tag);
        }

        protected virtual SqlExpression<T> CopyTo(SqlExpression<T> to)
        {
            to.modelDef = modelDef;
            to.tableDefs = tableDefs;

            to.UpdateFields = UpdateFields;
            to.InsertFields = InsertFields;

            to.selectExpression = selectExpression;
            to.OnlyFields = OnlyFields != null ? new HashSet<string>(OnlyFields, StringComparer.OrdinalIgnoreCase) : null;

            to.TableAlias = TableAlias;
            to.fromExpression = fromExpression;
            to.whereExpression = whereExpression;
            to.groupBy = groupBy;
            to.havingExpression = havingExpression;
            to.orderBy = orderBy;
            to.orderByProperties = orderByProperties;

            to.Offset = Offset;
            to.Rows = Rows;

            to.CustomSelect = CustomSelect;
            to.PrefixFieldWithTableName = PrefixFieldWithTableName;
            to.useFieldName = useFieldName;
            to.selectDistinct = selectDistinct;
            to.WhereStatementWithoutWhereString = WhereStatementWithoutWhereString;
            to.visitedExpressionIsTableColumn = visitedExpressionIsTableColumn;
            to.skipParameterizationForThisExpression = skipParameterizationForThisExpression;
            to.UseSelectPropertiesAsAliases = UseSelectPropertiesAsAliases;
            to.hasEnsureConditions = hasEnsureConditions;

            to.Params = new List<IDbDataParameter>(Params);

            to.underlyingExpression = underlyingExpression;
            to.SqlFilter = SqlFilter;
            
            return to;
        }

        /// <summary>
        /// Generate a unique SHA1 hash of expression with param values for caching 
        /// </summary>
        public string ComputeHash(bool includeParams=true)
        {
            var uniqueExpr = Dump(includeParams);
            // fastest up to 500 chars https://wintermute79.wordpress.com/2014/10/10/c-sha-1-benchmark/
            using var sha1 = System.Security.Cryptography.SHA1.Create();
            var hash = sha1.ComputeHash(Encoding.ASCII.GetBytes(uniqueExpr));
            var hexFormat = hash.ToHex();
            
            return hexFormat;
        }

        /// <summary>
        /// Dump internal state of this SqlExpression into a string
        /// </summary>
        /// <param name="includeParams"></param>
        /// <returns></returns>
        public string Dump(bool includeParams)
        {
            var sb = StringBuilderCache.Allocate();

            sb.Append('<').Append(ModelDef.Name);
            foreach (var tableDef in tableDefs)
            {
                sb.Append(',').Append(tableDef);
            }
            sb.Append('>').AppendLine();
            
            if (!UpdateFields.IsEmpty())
                sb.AppendLine(UpdateFields.Join(","));
            if (!InsertFields.IsEmpty())
                sb.AppendLine(InsertFields.Join(","));

            if (!string.IsNullOrEmpty(selectExpression))
                sb.AppendLine(selectExpression);
            if (!OnlyFields.IsEmpty())
                sb.AppendLine(OnlyFields.Join(","));

            if (!string.IsNullOrEmpty(TableAlias))
                sb.AppendLine(TableAlias);
            if (!string.IsNullOrEmpty(fromExpression))
                sb.AppendLine(fromExpression);

            if (!string.IsNullOrEmpty(whereExpression))
                sb.AppendLine(whereExpression);

            if (!string.IsNullOrEmpty(groupBy))
                sb.AppendLine(groupBy);

            if (!string.IsNullOrEmpty(havingExpression))
                sb.AppendLine(havingExpression);

            if (!string.IsNullOrEmpty(orderBy))
                sb.AppendLine(orderBy);
            if (!orderByProperties.IsEmpty())
                sb.AppendLine(orderByProperties.Join(","));

            if (Offset != null || Rows != null)
                sb.Append(Offset ?? 0).Append(',').Append(Rows ?? 0).AppendLine();

            sb.Append("FLAGS:");
            sb.Append(CustomSelect ? "1" : "0");
            sb.Append(PrefixFieldWithTableName ? "1" : "0");
            sb.Append(useFieldName ? "1" : "0");
            sb.Append(selectDistinct ? "1" : "0");
            sb.Append(WhereStatementWithoutWhereString ? "1" : "0");
            sb.Append(visitedExpressionIsTableColumn ? "1" : "0");
            sb.Append(skipParameterizationForThisExpression ? "1" : "0");
            sb.Append(UseSelectPropertiesAsAliases ? "1" : "0");
            sb.Append(hasEnsureConditions ? "1" : "0");
            sb.AppendLine();

            if (includeParams)
            {
                sb.Append("PARAMS:").Append(Params.Count).AppendLine();
                if (Params.Count > 0)
                {
                    foreach (var p in Params)
                    {
                        sb.Append(p.ParameterName).Append('=');
                        sb.AppendLine(p.Value.ConvertTo<string>());
                    }
                }
            }

            var uniqueExpr = StringBuilderCache.ReturnAndFree(sb);
            return uniqueExpr;
        }

        /// <summary>
        /// Clear select expression. All properties will be selected.
        /// </summary>
        public virtual SqlExpression<T> Select()
        {
            return Select(string.Empty);
        }

        internal SqlExpression<T> SelectIfDistinct(string selectExpression) => 
            selectDistinct ? SelectDistinct(selectExpression) : Select(selectExpression);

        /// <summary>
        /// set the specified selectExpression.
        /// </summary>
        /// <param name='selectExpression'>
        /// raw Select expression: "SomeField1, SomeField2 from SomeTable"
        /// </param>
        public virtual SqlExpression<T> Select(string selectExpression)
        {
            selectExpression?.SqlVerifyFragment();

            return UnsafeSelect(selectExpression);
        }

        /// <summary>
        /// set the specified DISTINCT selectExpression.
        /// </summary>
        /// <param name='selectExpression'>
        /// raw Select expression: "SomeField1, SomeField2 from SomeTable"
        /// </param>
        public virtual SqlExpression<T> SelectDistinct(string selectExpression)
        {
            selectExpression?.SqlVerifyFragment();

            return UnsafeSelect(selectExpression, distinct:true);
        }

        public virtual SqlExpression<T> UnsafeSelect(string rawSelect) => UnsafeSelect(rawSelect, distinct: false);

        public virtual SqlExpression<T> UnsafeSelect(string rawSelect, bool distinct)
        {
            if (string.IsNullOrEmpty(rawSelect))
            {
                BuildSelectExpression(string.Empty, distinct: distinct);
            }
            else
            {
                this.selectExpression = "SELECT " + (distinct ? "DISTINCT " : "") + rawSelect;
                this.CustomSelect = true;
                OnlyFields = null;
            }
            return this;
        }

        /// <summary>
        /// Set the specified selectExpression using matching fields.
        /// </summary>
        /// <param name='fields'>
        /// Matching Fields: "SomeField1, SomeField2"
        /// </param>
        public virtual SqlExpression<T> Select(string[] fields) => Select(fields, distinct: false);

        /// <summary>
        /// Set the specified DISTINCT selectExpression using matching fields.
        /// </summary>
        /// <param name='fields'>
        /// Matching Fields: "SomeField1, SomeField2"
        /// </param>
        public virtual SqlExpression<T> SelectDistinct(string[] fields) => Select(fields, distinct: true);

        internal virtual SqlExpression<T> Select(string[] fields, bool distinct)
        {
            if (fields == null || fields.Length == 0)
                return Select(string.Empty);

            useFieldName = true;

            var allTableDefs = GetAllTables();

            var fieldsList = new List<string>();
            var sb = StringBuilderCache.Allocate();
            foreach (var field in fields)
            {
                if (string.IsNullOrEmpty(field))
                    continue;

                if (field.EndsWith(".*"))
                {
                    var tableName = field.Substring(0, field.Length - 2);
                    var tableDef = allTableDefs.FirstOrDefault(x => string.Equals(x.Name, tableName, StringComparison.OrdinalIgnoreCase));
                    if (tableDef != null)
                    {
                        foreach (var fieldDef in tableDef.FieldDefinitionsArray)
                        {
                            var qualifiedField = GetQuotedColumnName(tableDef, fieldDef.Name);
                            if (fieldDef.CustomSelect != null)
                                qualifiedField += " AS " + fieldDef.Name;

                            if (sb.Length > 0)
                                sb.Append(", ");

                            sb.Append(qualifiedField);
                            fieldsList.Add(fieldDef.Name);
                        }
                    }
                }
                else
                {
                    fieldsList.Add(field); //Could be non-matching referenced property
    
                    var match = FirstMatchingField(field);
                    if (match == null)
                        continue;
    
                    var fieldDef = match.Item2;
                    var qualifiedName = GetQuotedColumnName(match.Item1, fieldDef.Name);
                    if (fieldDef.CustomSelect != null)
                        qualifiedName += " AS " + fieldDef.Name;
    
                    if (sb.Length > 0)
                        sb.Append(", ");
    
                    sb.Append(qualifiedName);
                }
            }

            UnsafeSelect(StringBuilderCache.ReturnAndFree(sb), distinct:distinct);
            OnlyFields = new HashSet<string>(fieldsList, StringComparer.OrdinalIgnoreCase);

            return this;
        }

        private SqlExpression<T> InternalSelect(Expression fields, bool distinct=false)
        {
            Reset(sep=string.Empty);

            CustomSelect = true;
            
            isSelectExpression = true;
            var selectSql = Visit(fields);
            isSelectExpression = false;
            
            if (!IsSqlClass(selectSql))
            {
                selectSql = ConvertToParam(selectSql);
            }
            BuildSelectExpression(selectSql.ToString(), distinct: distinct);
            return this;
        }

        /// <summary>
        /// Fields to be selected.
        /// </summary>
        /// <param name='fields'>
        /// x=> x.SomeProperty1 or x=> new{ x.SomeProperty1, x.SomeProperty2}
        /// </param>
        public virtual SqlExpression<T> Select(Expression<Func<T, object>> fields)
        {
            return InternalSelect(fields);
        }

        public virtual SqlExpression<T> Select<Table1>(Expression<Func<Table1, object>> fields)
        {
            return InternalSelect(fields);
        }

        public virtual SqlExpression<T> Select<Table1, Table2>(Expression<Func<Table1, Table2, object>> fields)
        {
            return InternalSelect(fields);
        }

        public virtual SqlExpression<T> Select<Table1, Table2, Table3>(Expression<Func<Table1, Table2, Table3, object>> fields)
        {
            return InternalSelect(fields);
        }

        public virtual SqlExpression<T> Select<Table1, Table2, Table3, Table4>(Expression<Func<Table1, Table2, Table3, Table4, object>> fields)
        {
            return InternalSelect(fields);
        }

        public virtual SqlExpression<T> Select<Table1, Table2, Table3, Table4, Table5>(Expression<Func<Table1, Table2, Table3, Table4, Table5, object>> fields)
        {
            return InternalSelect(fields);
        }

        public virtual SqlExpression<T> Select<Table1, Table2, Table3, Table4, Table5, Table6>(Expression<Func<Table1, Table2, Table3, Table4, Table5, Table6, object>> fields)
        {
            return InternalSelect(fields);
        }

        public virtual SqlExpression<T> Select<Table1, Table2, Table3, Table4, Table5, Table6, Table7>(Expression<Func<Table1, Table2, Table3, Table4, Table5, Table6, Table7, object>> fields)
        {
            return InternalSelect(fields);
        }

        public virtual SqlExpression<T> Select<Table1, Table2, Table3, Table4, Table5, Table6, Table7, Table8>(Expression<Func<Table1, Table2, Table3, Table4, Table5, Table6, Table7, Table8, object>> fields)
        {
            return InternalSelect(fields);
        }

        public virtual SqlExpression<T> Select<Table1, Table2, Table3, Table4, Table5, Table6, Table7, Table8, Table9>(Expression<Func<Table1, Table2, Table3, Table4, Table5, Table6, Table7, Table8, Table9, object>> fields)
        {
            return InternalSelect(fields);
        }

        public virtual SqlExpression<T> Select<Table1, Table2, Table3, Table4, Table5, Table6, Table7, Table8, Table9, Table10>(Expression<Func<Table1, Table2, Table3, Table4, Table5, Table6, Table7, Table8, Table9, Table10, object>> fields)
        {
            return InternalSelect(fields);
        }

        public virtual SqlExpression<T> Select<Table1, Table2, Table3, Table4, Table5, Table6, Table7, Table8, Table9, Table10, Table11>(Expression<Func<Table1, Table2, Table3, Table4, Table5, Table6, Table7, Table8, Table9, Table10, Table11, object>> fields)
        {
            return InternalSelect(fields);
        }

        public virtual SqlExpression<T> Select<Table1, Table2, Table3, Table4, Table5, Table6, Table7, Table8, Table9, Table10, Table11, Table12>(Expression<Func<Table1, Table2, Table3, Table4, Table5, Table6, Table7, Table8, Table9, Table10, Table11, Table12, object>> fields)
        {
            return InternalSelect(fields);
        }

        public virtual SqlExpression<T> SelectDistinct(Expression<Func<T, object>> fields)
        {
            return InternalSelect(fields, distinct:true);
        }

        public virtual SqlExpression<T> SelectDistinct<Table1>(Expression<Func<Table1, object>> fields)
        {
            return InternalSelect(fields, distinct: true);
        }

        public virtual SqlExpression<T> SelectDistinct<Table1, Table2>(Expression<Func<Table1, Table2, object>> fields)
        {
            return InternalSelect(fields, distinct: true);
        }

        public virtual SqlExpression<T> SelectDistinct<Table1, Table2, Table3>(Expression<Func<Table1, Table2, Table3, object>> fields)
        {
            return InternalSelect(fields, distinct: true);
        }

        public virtual SqlExpression<T> SelectDistinct<Table1, Table2, Table3, Table4>(Expression<Func<Table1, Table2, Table3, Table4, object>> fields)
        {
            return InternalSelect(fields, distinct: true);
        }

        public virtual SqlExpression<T> SelectDistinct<Table1, Table2, Table3, Table4, Table5>(Expression<Func<Table1, Table2, Table3, Table4, Table5, object>> fields)
        {
            return InternalSelect(fields, distinct: true);
        }

        public virtual SqlExpression<T> SelectDistinct<Table1, Table2, Table3, Table4, Table5, Table6>(Expression<Func<Table1, Table2, Table3, Table4, Table5, Table6, object>> fields)
        {
            return InternalSelect(fields, distinct: true);
        }

        public virtual SqlExpression<T> SelectDistinct<Table1, Table2, Table3, Table4, Table5, Table6, Table7>(Expression<Func<Table1, Table2, Table3, Table4, Table5, Table6, Table7, object>> fields)
        {
            return InternalSelect(fields, distinct: true);
        }

        public virtual SqlExpression<T> SelectDistinct<Table1, Table2, Table3, Table4, Table5, Table6, Table7, Table8>(Expression<Func<Table1, Table2, Table3, Table4, Table5, Table6, Table7, Table8, object>> fields)
        {
            return InternalSelect(fields, distinct: true);
        }

        public virtual SqlExpression<T> SelectDistinct<Table1, Table2, Table3, Table4, Table5, Table6, Table7, Table8, Table9>(Expression<Func<Table1, Table2, Table3, Table4, Table5, Table6, Table7, Table8, Table9, object>> fields)
        {
            return InternalSelect(fields, distinct: true);
        }

        public virtual SqlExpression<T> SelectDistinct<Table1, Table2, Table3, Table4, Table5, Table6, Table7, Table8, Table9, Table10>(Expression<Func<Table1, Table2, Table3, Table4, Table5, Table6, Table7, Table8, Table9, Table10, object>> fields)
        {
            return InternalSelect(fields, distinct: true);
        }

        public virtual SqlExpression<T> SelectDistinct<Table1, Table2, Table3, Table4, Table5, Table6, Table7, Table8, Table9, Table10, Table11>(Expression<Func<Table1, Table2, Table3, Table4, Table5, Table6, Table7, Table8, Table9, Table10, Table11, object>> fields)
        {
            return InternalSelect(fields, distinct: true);
        }

        public virtual SqlExpression<T> SelectDistinct<Table1, Table2, Table3, Table4, Table5, Table6, Table7, Table8, Table9, Table10, Table11, Table12>(Expression<Func<Table1, Table2, Table3, Table4, Table5, Table6, Table7, Table8, Table9, Table10, Table11, Table12, object>> fields)
        {
            return InternalSelect(fields, distinct: true);
        }

        public virtual SqlExpression<T> SelectDistinct()
        {
            selectDistinct = true;
            return this;
        }

        public virtual SqlExpression<T> From(string tables)
        {
            tables?.SqlVerifyFragment();

            return UnsafeFrom(tables);
        }

        public virtual SqlExpression<T> IncludeTablePrefix()
        {
            PrefixFieldWithTableName = true;
            return this;
        }

        public virtual SqlExpression<T> SetTableAlias(string tableAlias)
        {
            PrefixFieldWithTableName = tableAlias != null;
            TableAlias = tableAlias;
            return this;
        }

        public virtual SqlExpression<T> UnsafeFrom(string rawFrom)
        {
            if (string.IsNullOrEmpty(rawFrom))
            {
                FromExpression = null;
            }
            else
            {
                var singleTable = rawFrom.ToLower().IndexOfAny("join", ",") == -1;
                FromExpression = singleTable
                    ? " \nFROM " + DialectProvider.GetQuotedTableName(rawFrom)
                    : " \nFROM " + rawFrom;
            }

            return this;
        }

        public virtual SqlExpression<T> Where()
        {
            underlyingExpression = null; //Where() clears the expression

            whereExpression = null;
            return this;
        }

        private string FormatFilter(string sqlFilter, params object[] filterParams)
        {
            if (string.IsNullOrEmpty(sqlFilter))
                return null;

            for (var i = 0; i < filterParams.Length; i++)
            {
                var pLiteral = "{" + i + "}";
                var filterParam = filterParams[i];

                if (filterParam is SqlInValues sqlParams)
                {
                    if (sqlParams.Count > 0)
                    {
                        var sqlIn = CreateInParamSql(sqlParams.GetValues());
                        sqlFilter = sqlFilter.Replace(pLiteral, sqlIn);
                    }
                    else 
                    {
                        sqlFilter = sqlFilter.Replace(pLiteral, SqlInValues.EmptyIn);
                    }
                }
                else
                {
                    var p = AddParam(filterParam);
                    sqlFilter = sqlFilter.Replace(pLiteral, p.ParameterName);
                }
            }
            return sqlFilter;
        }

        private string CreateInParamSql(IEnumerable values)
        {
            var sbParams = StringBuilderCache.Allocate();
            foreach (var item in values)
            {
                var p = AddParam(item);

                if (sbParams.Length > 0)
                    sbParams.Append(",");

                sbParams.Append(p.ParameterName);
            }
            var sqlIn = StringBuilderCache.ReturnAndFree(sbParams);
            return sqlIn;
        }

        public virtual SqlExpression<T> UnsafeWhere(string rawSql, params object[] filterParams)
        {
            return AppendToWhere("AND", FormatFilter(rawSql, filterParams));
        }

        public virtual SqlExpression<T> Where(string sqlFilter, params object[] filterParams)
        {
            return AppendToWhere("AND", FormatFilter(sqlFilter.SqlVerifyFragment(), filterParams));
        }

        public virtual SqlExpression<T> UnsafeAnd(string rawSql, params object[] filterParams)
        {
            return AppendToWhere("AND", FormatFilter(rawSql, filterParams));
        }

        public virtual SqlExpression<T> And(string sqlFilter, params object[] filterParams)
        {
            return AppendToWhere("AND", FormatFilter(sqlFilter.SqlVerifyFragment(), filterParams));
        }

        public virtual SqlExpression<T> UnsafeOr(string rawSql, params object[] filterParams)
        {
            return AppendToWhere("OR", FormatFilter(rawSql, filterParams));
        }

        public virtual SqlExpression<T> Or(string sqlFilter, params object[] filterParams)
        {
            return AppendToWhere("OR", FormatFilter(sqlFilter.SqlVerifyFragment(), filterParams));
        }

        public virtual SqlExpression<T> AddCondition(string condition, string sqlFilter, params object[] filterParams)
        {
            return AppendToWhere(condition, FormatFilter(sqlFilter.SqlVerifyFragment(), filterParams));
        }

        public virtual SqlExpression<T> Where(Expression<Func<T, bool>> predicate) => AppendToWhere("AND", predicate);
        public virtual SqlExpression<T> Where(Expression<Func<T, bool>> predicate, params object[] filterParams) => 
            AppendToWhere("AND", predicate, filterParams);

        public virtual SqlExpression<T> And(Expression<Func<T, bool>> predicate) => AppendToWhere("AND", predicate);
        public virtual SqlExpression<T> And(Expression<Func<T, bool>> predicate, params object[] filterParams) => 
            AppendToWhere("AND", predicate, filterParams);

        public virtual SqlExpression<T> Or(Expression<Func<T, bool>> predicate) => AppendToWhere("OR", predicate);

        public virtual SqlExpression<T> Or(Expression<Func<T, bool>> predicate, params object[] filterParams) => 
            AppendToWhere("OR", predicate, filterParams);


        public virtual SqlExpression<T> WhereExists(ISqlExpression subSelect)
        {
            return AppendToWhere("AND", FormatFilter($"EXISTS ({subSelect.ToSelectStatement()})"));
        }
        public virtual SqlExpression<T> WhereNotExists(ISqlExpression subSelect)
        {
            return AppendToWhere("AND", FormatFilter($"NOT EXISTS ({subSelect.ToSelectStatement()})"));
        }
        
        private LambdaExpression originalLambda;

        void Reset(string sep = " ", bool useFieldName = true)
        {
            this.sep = sep;
            this.useFieldName = useFieldName;
            this.originalLambda = null;
        }

        protected SqlExpression<T> AppendToWhere(string condition, Expression predicate, object[] filterParams)
        {
            if (predicate == null)
                return this;

            Reset();

            var newExpr = WhereExpressionToString(Visit(predicate));
            var formatExpr = FormatFilter(newExpr, filterParams);
            return AppendToWhere(condition, formatExpr);
        }
        
        protected SqlExpression<T> AppendToWhere(string condition, Expression predicate)
        {
            if (predicate == null)
                return this;

            Reset();

            var newExpr = WhereExpressionToString(Visit(predicate));
            return AppendToWhere(condition, newExpr);
        }

        private static string WhereExpressionToString(object expression)
        {
            if (expression is bool b)
                return b ? TrueLiteral : FalseLiteral;
            return expression.ToString();
        }

        protected SqlExpression<T> AppendToWhere(string condition, string sqlExpression)
        {
            var addExpression = string.IsNullOrEmpty(whereExpression)
                ? (WhereStatementWithoutWhereString ? "" : "WHERE ") + sqlExpression
                : " " + condition + " " + sqlExpression;

            if (!hasEnsureConditions)
            {
                whereExpression += addExpression;
            }
            else
            {
                if (whereExpression[whereExpression.Length - 1] != ')')
                    throw new NotSupportedException("Invalid whereExpression Expression with Ensure Conditions");

                // insert before normal WHERE parens: {EnsureConditions} AND (1+1)
                if (whereExpression.EndsWith(TrueLiteral, StringComparison.Ordinal)) // insert before ^1+1)
                {
                    whereExpression = whereExpression.Substring(0, whereExpression.Length - (TrueLiteral.Length - 1))
                                    + sqlExpression + ")";
                }
                else // insert before ^)
                {
                    whereExpression = whereExpression.Substring(0, whereExpression.Length - 1) 
                                    + addExpression + ")";
                }
            }
            return this;
        }

        public virtual SqlExpression<T> Ensure(Expression<Func<T, bool>> predicate) => AppendToEnsure(predicate);
        public virtual SqlExpression<T> Ensure<Target>(Expression<Func<Target, bool>> predicate) => AppendToEnsure(predicate);
        public virtual SqlExpression<T> Ensure<Source, Target>(Expression<Func<Source, Target, bool>> predicate) => AppendToEnsure(predicate);
        public virtual SqlExpression<T> Ensure<T1, T2, T3>(Expression<Func<T1, T2, T3, bool>> predicate) => AppendToEnsure(predicate);
        public virtual SqlExpression<T> Ensure<T1, T2, T3, T4>(Expression<Func<T1, T2, T3, T4, bool>> predicate) => AppendToEnsure(predicate);
        public virtual SqlExpression<T> Ensure<T1, T2, T3, T4, T5>(Expression<Func<T1, T2, T3, T4, T5, bool>> predicate) => AppendToEnsure(predicate);
        
        protected SqlExpression<T> AppendToEnsure(Expression predicate)
        {
            if (predicate == null)
                return this;

            Reset();

            var newExpr = WhereExpressionToString(Visit(predicate));
            return Ensure(newExpr);
        }

        /// <summary>
        /// Add a WHERE Condition to always be applied, irrespective of other WHERE conditions 
        /// </summary>
        public SqlExpression<T> Ensure(string sqlFilter, params object[] filterParams)
        {
            var condition = FormatFilter(sqlFilter, filterParams);
            if (string.IsNullOrEmpty(whereExpression))
            {
                whereExpression = "WHERE " + condition 
                    + " AND " + TrueLiteral; //allow subsequent WHERE conditions to be inserted before parens
            }
            else
            {
                if (!hasEnsureConditions)
                {
                    var existingExpr = whereExpression.StartsWith("WHERE ", StringComparison.OrdinalIgnoreCase)
                        ? whereExpression.Substring("WHERE ".Length)
                        : whereExpression;

                    whereExpression = "WHERE " + condition + " AND (" + existingExpr + ")";
                }
                else
                {
                    if (!whereExpression.StartsWith("WHERE ", StringComparison.OrdinalIgnoreCase))
                        throw new NotSupportedException("Invalid whereExpression Expression with Ensure Conditions");

                    whereExpression = "WHERE " + condition + " AND " + whereExpression.Substring("WHERE ".Length);
                }
            }

            hasEnsureConditions = true;
            return this;
        }

        private string ListExpression(Expression expr, string strExpr)
        {
            if (expr is LambdaExpression lambdaExpr)
            {
                if (lambdaExpr.Parameters.Count == 1 && lambdaExpr.Body is MemberExpression me)
                {
                    var tableDef = lambdaExpr.Parameters[0].Type.GetModelMetadata();
                    var fieldDef = tableDef?.GetFieldDefinition(me.Member.Name);
                    if (fieldDef != null)
                        return DialectProvider.GetQuotedColumnName(tableDef, me.Member.Name);
                }
            }
            return strExpr;
        }

        public virtual SqlExpression<T> GroupBy()
        {
            return GroupBy(string.Empty);
        }

        public virtual SqlExpression<T> GroupBy(string groupBy)
        {
            return UnsafeGroupBy(groupBy.SqlVerifyFragment());
        }

        public virtual SqlExpression<T> UnsafeGroupBy(string groupBy)
        {
            if (!string.IsNullOrEmpty(groupBy))
                this.groupBy = "GROUP BY " + groupBy;
            return this;
        }

        private SqlExpression<T> InternalGroupBy(Expression expr)
        {
            Reset(sep=string.Empty);

            var groupByExpr = Visit(expr);
            if (IsSqlClass(groupByExpr))
            {
                StripAliases(groupByExpr as SelectList); // No "AS ColumnAlias" in GROUP BY, just the column names/expressions

                return GroupBy(groupByExpr.ToString());
            }
            if (groupByExpr is string strExpr)
            {
                return GroupBy(ListExpression(expr, strExpr));
            }

            return this;
        }

        public virtual SqlExpression<T> GroupBy<Table>(Expression<Func<Table, object>> keySelector)
        {
            return InternalGroupBy(keySelector);
        }

        public virtual SqlExpression<T> GroupBy<Table1, Table2>(Expression<Func<Table1, Table2, object>> keySelector)
        {
            return InternalGroupBy(keySelector);
        }

        public virtual SqlExpression<T> GroupBy<Table1, Table2, Table3>(Expression<Func<Table1, Table2, Table3, object>> keySelector)
        {
            return InternalGroupBy(keySelector);
        }

        public virtual SqlExpression<T> GroupBy<Table1, Table2, Table3, Table4>(Expression<Func<Table1, Table2, Table3, Table4, object>> keySelector)
        {
            return InternalGroupBy(keySelector);
        }

        public virtual SqlExpression<T> GroupBy(Expression<Func<T, object>> keySelector)
        {
            return InternalGroupBy(keySelector);
        }

        public virtual SqlExpression<T> Having()
        {
            return Having(string.Empty);
        }

        public virtual SqlExpression<T> Having(string sqlFilter, params object[] filterParams)
        {
            havingExpression = FormatFilter(sqlFilter.SqlVerifyFragment(), filterParams);

            if (havingExpression != null)
                havingExpression = "HAVING " + havingExpression;

            return this;
        }

        public virtual SqlExpression<T> UnsafeHaving(string sqlFilter, params object[] filterParams)
        {
            havingExpression = FormatFilter(sqlFilter, filterParams);

            if (havingExpression != null)
                havingExpression = "HAVING " + havingExpression;

            return this;
        }

        protected SqlExpression<T> AppendHaving(Expression predicate)
        {
            if (predicate != null)
            {
                Reset();

                havingExpression = WhereExpressionToString(Visit(predicate));
                if (!string.IsNullOrEmpty(havingExpression))
                    havingExpression = "HAVING " + havingExpression;
            }
            else
                havingExpression = string.Empty;

            return this;
        }

        public virtual SqlExpression<T> Having(Expression<Func<T, bool>> predicate) => AppendHaving(predicate);
        public virtual SqlExpression<T> Having<Table>(Expression<Func<Table, bool>> predicate) => AppendHaving(predicate);
        public virtual SqlExpression<T> Having<Table1, Table2>(Expression<Func<Table1, Table2, bool>> predicate) => AppendHaving(predicate);
        public virtual SqlExpression<T> Having<Table1, Table2, Table3>(Expression<Func<Table1, Table2, Table3, bool>> predicate) => AppendHaving(predicate);

        public virtual SqlExpression<T> OrderBy() => OrderBy(string.Empty);

        public virtual SqlExpression<T> OrderBy(string orderBy) => UnsafeOrderBy(orderBy.SqlVerifyFragment());

        public virtual SqlExpression<T> OrderBy(long columnIndex) => UnsafeOrderBy(columnIndex.ToString());

        public virtual SqlExpression<T> UnsafeOrderBy(string orderBy)
        {
            orderByProperties.Clear();
            if (!string.IsNullOrEmpty(orderBy))
            {
                orderByProperties.Add(orderBy);
            }
            BuildOrderByClauseInternal();
            return this;
        }

        public virtual SqlExpression<T> OrderByRandom() => OrderBy(DialectProvider.SqlRandom);

        public ModelDefinition GetModelDefinition(FieldDefinition fieldDef)
        {
            if (modelDef.FieldDefinitions.Any(x => x == fieldDef))
                return modelDef;

            return tableDefs
                .FirstOrDefault(tableDef => tableDef.FieldDefinitions.Any(x => x == fieldDef));
        }

        private SqlExpression<T> OrderByFields(string orderBySuffix, FieldDefinition[] fields)
        {
            orderByProperties.Clear();

            if (fields.Length == 0)
            {
                this.orderBy = null;
                return this;
            }

            useFieldName = true;

            var sbOrderBy = StringBuilderCache.Allocate();
            foreach (var field in fields)
            {
                var tableDef = GetModelDefinition(field);
                var qualifiedName = tableDef != null
                    ? GetQuotedColumnName(tableDef, field.Name)
                    : DialectProvider.GetQuotedColumnName(field);

                if (sbOrderBy.Length > 0)
                    sbOrderBy.Append(", ");

                sbOrderBy.Append(qualifiedName + orderBySuffix);
            }

            this.orderBy = "ORDER BY " + StringBuilderCache.ReturnAndFree(sbOrderBy);
            return this;
        }

        static class OrderBySuffix
        {
            public const string Asc = "";
            public const string Desc = " DESC";
        }

        public virtual SqlExpression<T> OrderByFields(params FieldDefinition[] fields) => OrderByFields(OrderBySuffix.Asc, fields);

        public virtual SqlExpression<T> OrderByFieldsDescending(params FieldDefinition[] fields) => OrderByFields(OrderBySuffix.Desc, fields);

        private SqlExpression<T> OrderByFields(string orderBySuffix, string[] fieldNames)
        {
            orderByProperties.Clear();

            if (fieldNames.Length == 0)
            {
                this.orderBy = null;
                return this;
            }

            useFieldName = true;

            var sbOrderBy = StringBuilderCache.Allocate();
            foreach (var fieldName in fieldNames)
            {
                var reverse = fieldName.StartsWith("-");
                var useSuffix = reverse
                    ? (orderBySuffix == OrderBySuffix.Asc ? OrderBySuffix.Desc : OrderBySuffix.Asc)
                    : orderBySuffix;
                var useName = reverse ? fieldName.Substring(1) : fieldName;

                var field = FirstMatchingField(useName);
                var qualifiedName = field != null
                    ? GetQuotedColumnName(field.Item1, field.Item2.Name)
                    : string.Equals("Random", useName, StringComparison.OrdinalIgnoreCase)
                        ? DialectProvider.SqlRandom
                        : throw new ArgumentException("Could not find field " + useName);

                if (sbOrderBy.Length > 0)
                    sbOrderBy.Append(", ");

                sbOrderBy.Append(qualifiedName + useSuffix);
            }

            this.orderBy = "ORDER BY " + StringBuilderCache.ReturnAndFree(sbOrderBy);
            return this;
        }

        public virtual SqlExpression<T> OrderByFields(params string[] fieldNames) => OrderByFields("", fieldNames);

        public virtual SqlExpression<T> OrderByFieldsDescending(params string[] fieldNames) => OrderByFields(" DESC", fieldNames);

        public virtual SqlExpression<T> OrderBy(Expression<Func<T, object>> keySelector) => OrderByInternal(keySelector);

        public virtual SqlExpression<T> OrderBy<Table>(Expression<Func<Table, object>> fields) => OrderByInternal(fields);
        public virtual SqlExpression<T> OrderBy<Table1, Table2>(Expression<Func<Table1, Table2, object>> fields) => OrderByInternal(fields);
        public virtual SqlExpression<T> OrderBy<Table1, Table2, Table3>(Expression<Func<Table1, Table2, Table3, object>> fields) => OrderByInternal(fields);
        public virtual SqlExpression<T> OrderBy<Table1, Table2, Table3, Table4>(Expression<Func<Table1, Table2, Table3, Table4, object>> fields) => OrderByInternal(fields);
        public virtual SqlExpression<T> OrderBy<Table1, Table2, Table3, Table4, Table5>(Expression<Func<Table1, Table2, Table3, Table4, Table5, object>> fields) => OrderByInternal(fields);

        private SqlExpression<T> OrderByInternal(Expression expr)
        {
            Reset(sep=string.Empty);

            orderByProperties.Clear();
            var orderBySql = Visit(expr);
            if (IsSqlClass(orderBySql))
            {
                var fields = orderBySql.ToString();
                orderByProperties.Add(fields);
                BuildOrderByClauseInternal();
            }
            else if (orderBySql is string strExpr)
            {
                return GroupBy(ListExpression(expr, strExpr));
            }
            return this;
        }

        public static bool IsSqlClass(object obj)
        {
            return obj != null &&
                   (obj is PartialSqlString ||
                    obj is SelectList);
        }

        public virtual SqlExpression<T> ThenBy(string orderBy)
        {
            orderBy.SqlVerifyFragment();
            orderByProperties.Add(orderBy);
            BuildOrderByClauseInternal();
            return this;
        }

        public virtual SqlExpression<T> ThenBy(Expression<Func<T, object>> keySelector) => ThenByInternal(keySelector);
        public virtual SqlExpression<T> ThenBy<Table>(Expression<Func<Table, object>> fields) => ThenByInternal(fields);
        public virtual SqlExpression<T> ThenBy<Table1, Table2>(Expression<Func<Table1, Table2, object>> fields) => ThenByInternal(fields);
        public virtual SqlExpression<T> ThenBy<Table1, Table2, Table3>(Expression<Func<Table1, Table2, Table3, object>> fields) => ThenByInternal(fields);
        public virtual SqlExpression<T> ThenBy<Table1, Table2, Table3, Table4>(Expression<Func<Table1, Table2, Table3, Table4, object>> fields) => ThenByInternal(fields);
        public virtual SqlExpression<T> ThenBy<Table1, Table2, Table3, Table4, Table5>(Expression<Func<Table1, Table2, Table3, Table4, Table5, object>> fields) => ThenByInternal(fields);

        private SqlExpression<T> ThenByInternal(Expression keySelector)
        {
            Reset(sep=string.Empty);

            var orderBySql = Visit(keySelector);
            if (IsSqlClass(orderBySql))
            {
                var fields = orderBySql.ToString();
                orderByProperties.Add(fields);
                BuildOrderByClauseInternal();
            }
            return this;
        }

        public virtual SqlExpression<T> OrderByDescending(Expression<Func<T, object>> keySelector)
        {
            return OrderByDescendingInternal(keySelector);
        }

        public virtual SqlExpression<T> OrderByDescending<Table>(Expression<Func<Table, object>> keySelector) => OrderByDescendingInternal(keySelector);
        public virtual SqlExpression<T> OrderByDescending<Table1, Table2>(Expression<Func<Table1, Table2, object>> fields) => OrderByDescendingInternal(fields);
        public virtual SqlExpression<T> OrderByDescending<Table1, Table2, Table3>(Expression<Func<Table1, Table2, Table3, object>> fields) => OrderByDescendingInternal(fields);
        public virtual SqlExpression<T> OrderByDescending<Table1, Table2, Table3, Table4>(Expression<Func<Table1, Table2, Table3, Table4, object>> fields) => OrderByDescendingInternal(fields);
        public virtual SqlExpression<T> OrderByDescending<Table1, Table2, Table3, Table4, Table5>(Expression<Func<Table1, Table2, Table3, Table4, Table5, object>> fields) => OrderByDescendingInternal(fields);

        private SqlExpression<T> OrderByDescendingInternal(Expression keySelector)
        {
            Reset(sep=string.Empty);

            orderByProperties.Clear();
            var orderBySql = Visit(keySelector);
            if (IsSqlClass(orderBySql))
            {
                var fields = orderBySql.ToString();
                fields.ParseTokens()
                    .Each(x => orderByProperties.Add(x + " DESC"));
                BuildOrderByClauseInternal();
            }
            return this;
        }

        public virtual SqlExpression<T> OrderByDescending(string orderBy) => UnsafeOrderByDescending(orderBy.SqlVerifyFragment());

        public virtual SqlExpression<T> OrderByDescending(long columnIndex) => UnsafeOrderByDescending(columnIndex.ToString());

        private SqlExpression<T> UnsafeOrderByDescending(string orderBy)
        {
            orderByProperties.Clear();
            orderByProperties.Add(orderBy + " DESC");
            BuildOrderByClauseInternal();
            return this;
        }

        public virtual SqlExpression<T> ThenByDescending(string orderBy)
        {
            orderBy.SqlVerifyFragment();
            orderByProperties.Add(orderBy + " DESC");
            BuildOrderByClauseInternal();
            return this;
        }

        public virtual SqlExpression<T> ThenByDescending(Expression<Func<T, object>> keySelector) => ThenByDescendingInternal(keySelector);
        public virtual SqlExpression<T> ThenByDescending<Table>(Expression<Func<Table, object>> fields) => ThenByDescendingInternal(fields);
        public virtual SqlExpression<T> ThenByDescending<Table1, Table2>(Expression<Func<Table1, Table2, object>> fields) => ThenByDescendingInternal(fields);
        public virtual SqlExpression<T> ThenByDescending<Table1, Table2, Table3>(Expression<Func<Table1, Table2, Table3, object>> fields) => ThenByDescendingInternal(fields);
        public virtual SqlExpression<T> ThenByDescending<Table1, Table2, Table3, Table4>(Expression<Func<Table1, Table2, Table3, Table4, object>> fields) => ThenByDescendingInternal(fields);
        public virtual SqlExpression<T> ThenByDescending<Table1, Table2, Table3, Table4, Table5>(Expression<Func<Table1, Table2, Table3, Table4, Table5, object>> fields) => ThenByDescendingInternal(fields);

        private SqlExpression<T> ThenByDescendingInternal(Expression keySelector)
        {
            Reset(sep=string.Empty);

            var orderBySql = Visit(keySelector);
            if (IsSqlClass(orderBySql))
            {
                var fields = orderBySql.ToString();
                fields.ParseTokens()
                    .Each(x => orderByProperties.Add(x + " DESC"));
                BuildOrderByClauseInternal();
            }
            return this;
        }

        private void BuildOrderByClauseInternal()
        {
            if (orderByProperties.Count > 0)
            {
                var sb = StringBuilderCache.Allocate();
                foreach (var prop in orderByProperties)
                {
                    if (sb.Length > 0)
                        sb.Append(", ");

                    sb.Append(prop);
                }
                orderBy = "ORDER BY " + StringBuilderCache.ReturnAndFree(sb);
            }
            else
            {
                orderBy = null;
            }
        }

        /// <summary>
        /// Offset of the first row to return. The offset of the initial row is 0
        /// </summary>
        public virtual SqlExpression<T> Skip(int? skip = null)
        {
            Offset = skip;
            return this;
        }

        /// <summary>
        /// Number of rows returned by a SELECT statement
        /// </summary>
        public virtual SqlExpression<T> Take(int? take = null)
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
        public virtual SqlExpression<T> Limit(int skip, int rows)
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
        public virtual SqlExpression<T> Limit(int? skip, int? rows)
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
        public virtual SqlExpression<T> Limit(int rows)
        {
            Offset = null;
            Rows = rows;
            return this;
        }

        /// <summary>
        /// Clear Sql Limit clause
        /// </summary>
        public virtual SqlExpression<T> Limit()
        {
            Offset = null;
            Rows = null;
            return this;
        }

        /// <summary>
        /// Clear Offset and Limit clauses. Alias for Limit()
        /// </summary>
        /// <returns></returns>
        public virtual SqlExpression<T> ClearLimits()
        {
            return Limit();
        }

        /// <summary>
        /// Fields to be updated.
        /// </summary>
        /// <param name='updateFields'>
        /// List&lt;string&gt; containing Names of properties to be updated
        /// </param>
        public virtual SqlExpression<T> Update(List<string> updateFields)
        {
            this.UpdateFields = updateFields;
            return this;
        }

        /// <summary>
        /// Fields to be updated.
        /// </summary>
        /// <param name='updateFields'>
        /// IEnumerable&lt;string&gt; containing Names of properties to be updated
        /// </param>
        public virtual SqlExpression<T> Update(IEnumerable<string> updateFields)
        {
            this.UpdateFields = new List<string>(updateFields);
            return this;
        }

        /// <summary>
        /// Fields to be updated.
        /// </summary>
        /// <param name='fields'>
        /// x=> x.SomeProperty1 or x=> new { x.SomeProperty1, x.SomeProperty2 }
        /// </param>
        public virtual SqlExpression<T> Update(Expression<Func<T, object>> fields)
        {
            Reset(sep=string.Empty, useFieldName=false);
            this.UpdateFields = fields.GetFieldNames().ToList();
            return this;
        }

        /// <summary>
        /// Clear UpdateFields list ( all fields will be updated)
        /// </summary>
        public virtual SqlExpression<T> Update()
        {
            this.UpdateFields = new List<string>();
            return this;
        }

        /// <summary>
        /// Fields to be inserted.
        /// </summary>
        /// <param name='fields'>
        /// x=> x.SomeProperty1 or x=> new{ x.SomeProperty1, x.SomeProperty2}
        /// </param>
        /// <typeparam name='TKey'>
        /// objectWithProperties
        /// </typeparam>
        public virtual SqlExpression<T> Insert<TKey>(Expression<Func<T, TKey>> fields)
        {
            Reset(sep=string.Empty, useFieldName=false);
            var fieldList = Visit(fields);
            InsertFields = fieldList.ToString().Split(',').Select(f => f.Trim()).ToList();
            return this;
        }

        /// <summary>
        /// fields to be inserted.
        /// </summary>
        /// <param name='insertFields'>
        /// IList&lt;string&gt; containing Names of properties to be inserted
        /// </param>
        public virtual SqlExpression<T> Insert(List<string> insertFields)
        {
            this.InsertFields = insertFields;
            return this;
        }

        /// <summary>
        /// Clear InsertFields list ( all fields will be inserted)
        /// </summary>
        public virtual SqlExpression<T> Insert()
        {
            this.InsertFields = new List<string>();
            return this;
        }

        public virtual SqlExpression<T> WithSqlFilter(Func<string,string> sqlFilter)
        {
            this.SqlFilter = sqlFilter;
            return this;
        }

        public string SqlTable(ModelDefinition modelDef)
        {
            return DialectProvider.GetQuotedTableName(modelDef);
        }

        public string SqlColumn(string columnName)
        {
            return DialectProvider.GetQuotedColumnName(columnName);
        }

        public virtual IDbDataParameter AddParam(object value)
        {
            var paramName = Params.Count.ToString();
            var paramValue = value;

            var parameter = CreateParam(paramName, paramValue);
            DialectProvider.InitQueryParam(parameter);
            Params.Add(parameter);
            return parameter;
        }

        public string ConvertToParam(object value)
        {
            var p = AddParam(value);
            return p.ParameterName;
        }

        public virtual void CopyParamsTo(IDbCommand dbCmd)
        {
            try
            {
                foreach (var sqlParam in Params)
                {
                    dbCmd.Parameters.Add(sqlParam);
                }
            }
            catch (Exception)
            {
                //SQL Server + PostgreSql doesn't allow re-using db params in multiple queries
                foreach (var sqlParam in Params)
                {
                    var p = dbCmd.CreateParameter();
                    p.PopulateWith(sqlParam);
                    dbCmd.Parameters.Add(p);
                }
            }
        }

        public virtual string ToDeleteRowStatement()
        {
            string sql;
            var hasTableJoin = tableDefs.Count > 1;
            if (hasTableJoin)
            {
                var clone = this.Clone();
                var pk = DialectProvider.GetQuotedColumnName(modelDef, modelDef.PrimaryKey);
                clone.Select(pk);
                var subSql = clone.ToSelectStatement(QueryType.Select);
                sql = $"DELETE FROM {DialectProvider.GetQuotedTableName(modelDef)} WHERE {pk} IN ({subSql})";
            }
            else
            {
                sql = $"DELETE FROM {DialectProvider.GetQuotedTableName(modelDef)} {WhereExpression}";
            }

            return SqlFilter != null
                ? SqlFilter(sql)
                : sql;
        }

        public virtual void PrepareUpdateStatement(IDbCommand dbCmd, T item, bool excludeDefaults = false)
        {
            CopyParamsTo(dbCmd);

            var setFields = StringBuilderCache.Allocate();

            foreach (var fieldDef in modelDef.FieldDefinitions)
            {
                if (fieldDef.ShouldSkipUpdate()) 
                    continue;
                if (fieldDef.IsRowVersion) 
                    continue;
                if (UpdateFields.Count > 0
                    && !UpdateFields.Contains(fieldDef.Name)) continue; // added

                var value = fieldDef.GetValue(item);
                if (excludeDefaults
                    && (value == null || (!fieldDef.IsNullable && value.Equals(value.GetType().GetDefaultValue()))))
                    continue;

                if (setFields.Length > 0)
                    setFields.Append(", ");

                setFields
                    .Append(DialectProvider.GetQuotedColumnName(fieldDef.FieldName))
                    .Append("=")
                    .Append(DialectProvider.GetUpdateParam(dbCmd, value, fieldDef));
            }

            if (setFields.Length == 0)
                throw new ArgumentException($"No non-null or non-default values were provided for type: {typeof(T).Name}");

            var sql = $"UPDATE {DialectProvider.GetQuotedTableName(modelDef)} " +
                      $"SET {StringBuilderCache.ReturnAndFree(setFields)} {WhereExpression}";

            dbCmd.CommandText = SqlFilter != null
                ? SqlFilter(sql)
                : sql;
        }

        public virtual void PrepareUpdateStatement(IDbCommand dbCmd, Dictionary<string, object> updateFields)
        {
            CopyParamsTo(dbCmd);

            var setFields = StringBuilderCache.Allocate();

            foreach (var entry in updateFields)
            {
                var fieldDef = ModelDef.AssertFieldDefinition(entry.Key);
                if (fieldDef.ShouldSkipUpdate()) 
                    continue;
                if (fieldDef.IsRowVersion) 
                    continue;

                if (UpdateFields.Count > 0
                    && !UpdateFields.Contains(fieldDef.Name)) // added 
                    continue;

                var value = entry.Value;
                if (value == null && !fieldDef.IsNullable)
                    continue;

                if (setFields.Length > 0)
                    setFields.Append(", ");

                setFields
                    .Append(DialectProvider.GetQuotedColumnName(fieldDef.FieldName))
                    .Append("=")
                    .Append(DialectProvider.GetUpdateParam(dbCmd, value, fieldDef));
            }
            
            if (setFields.Length == 0)
                throw new ArgumentException($"No non-null or non-default values were provided for type: {typeof(T).Name}");

            var sql = $"UPDATE {DialectProvider.GetQuotedTableName(modelDef)} " +
                      $"SET {StringBuilderCache.ReturnAndFree(setFields)} {WhereExpression}";

            dbCmd.CommandText = SqlFilter != null
                ? SqlFilter(sql)
                : sql;
        }

        public virtual string ToSelectStatement() => ToSelectStatement(QueryType.Select);

        public virtual string ToSelectStatement(QueryType forType)
        {
            SelectFilter?.Invoke(this);
            OrmLiteConfig.SqlExpressionSelectFilter?.Invoke(GetUntyped());

            var sql = DialectProvider
                .ToSelectStatement(forType, modelDef, SelectExpression, BodyExpression, OrderByExpression, offset: Offset, rows: Rows,Tags);

            return SqlFilter != null
                ? SqlFilter(sql)
                : sql;
        }

        /// <summary>
        /// Merge params into an encapsulated SQL Statement with embedded param values
        /// </summary>
        public virtual string ToMergedParamsSelectStatement()
        {
            var sql = this.ToSelectStatement(QueryType.Select);
            var mergedSql = DialectProvider.MergeParamsIntoSql(sql, Params);
            return mergedSql;
        }

        public virtual string ToCountStatement()
        {
            SelectFilter?.Invoke(this);
            OrmLiteConfig.SqlExpressionSelectFilter?.Invoke(GetUntyped());

            var sql = "SELECT COUNT(*)" + BodyExpression;

            return SqlFilter != null
                ? SqlFilter(sql)
                : sql;
        }

        public string SelectExpression
        {
            get
            {
                if (string.IsNullOrEmpty(selectExpression))
                    BuildSelectExpression(string.Empty, false);
                return selectExpression;
            }
            set => selectExpression = value;
        }

        public string FromExpression
        {
            get => string.IsNullOrEmpty(fromExpression)
                ? " \nFROM " + DialectProvider.GetQuotedTableName(modelDef) + (TableAlias != null ? " " + DialectProvider.GetQuotedName(TableAlias) : "")
                : fromExpression;
            set => fromExpression = value;
        }

        public string BodyExpression => FromExpression
            + (string.IsNullOrEmpty(WhereExpression) ? "" : "\n" + WhereExpression)
            + (string.IsNullOrEmpty(GroupByExpression) ? "" : "\n" + GroupByExpression)
            + (string.IsNullOrEmpty(HavingExpression) ? "" : "\n" + HavingExpression);

        public string WhereExpression
        {
            get => whereExpression;
            set => whereExpression = value;
        }

        public string GroupByExpression
        {
            get => groupBy;
            set => groupBy = value;
        }

        public string HavingExpression
        {
            get => havingExpression;
            set => havingExpression = value;
        }


        public string OrderByExpression
        {
            get => string.IsNullOrEmpty(orderBy) ? "" : "\n" + orderBy;
            set => orderBy = value;
        }

        public ModelDefinition ModelDef
        {
            get => modelDef;
            protected set => modelDef = value;
        }

        protected internal bool UseFieldName
        {
            get => useFieldName;
            set => useFieldName = value;
        }

        public virtual object Visit(Expression exp)
        {
            visitedExpressionIsTableColumn = false;

            if (exp == null)
                return string.Empty;

            switch (exp.NodeType)
            {
                case ExpressionType.Lambda:
                    return VisitLambda(exp as LambdaExpression);
                case ExpressionType.MemberAccess:
                    return VisitMemberAccess(exp as MemberExpression);
                case ExpressionType.Constant:
                    return VisitConstant(exp as ConstantExpression);
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                case ExpressionType.Divide:
                case ExpressionType.Modulo:
                case ExpressionType.AndAlso:
                case ExpressionType.OrElse:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.Coalesce:
                case ExpressionType.ArrayIndex:
                case ExpressionType.And:
                case ExpressionType.Or:
                case ExpressionType.ExclusiveOr:
                case ExpressionType.RightShift:
                case ExpressionType.LeftShift:
                    //return "(" + VisitBinary(exp as BinaryExpression) + ")";
                    return VisitBinary(exp as BinaryExpression);
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                case ExpressionType.Not:
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.ArrayLength:
                case ExpressionType.Quote:
                case ExpressionType.TypeAs:
                    return VisitUnary(exp as UnaryExpression);
                case ExpressionType.Parameter:
                    return VisitParameter(exp as ParameterExpression);
                case ExpressionType.Call:
                    return VisitMethodCall(exp as MethodCallExpression);
                case ExpressionType.New:
                    return VisitNew(exp as NewExpression);
                case ExpressionType.NewArrayInit:
                case ExpressionType.NewArrayBounds:
                    return VisitNewArray(exp as NewArrayExpression);
                case ExpressionType.MemberInit:
                    return VisitMemberInit(exp as MemberInitExpression);
                case ExpressionType.Index:
                    return VisitIndexExpression(exp as IndexExpression);
                case ExpressionType.Conditional:
                    return VisitConditional(exp as ConditionalExpression);
                default:
                    return exp.ToString();
            }
        }

        protected virtual object VisitJoin(Expression exp)
        {
            skipParameterizationForThisExpression = true;
            var visitedExpression = Visit(exp);
            skipParameterizationForThisExpression = false;
            return visitedExpression;
        }

        protected virtual object VisitLambda(LambdaExpression lambda)
        {
            if (originalLambda == null)
                originalLambda = lambda;
            
            if (lambda.Body.NodeType == ExpressionType.MemberAccess && sep == " ")
            {
                MemberExpression m = lambda.Body as MemberExpression;

                if (m.Expression != null)
                {
                    var r = VisitMemberAccess(m);
                    if (!(r is PartialSqlString))
                        return r;

                    if (m.Expression.Type.IsNullableType())
                        return r.ToString();

                    return $"{r}={GetQuotedTrueValue()}";
                }

            }
            else if (lambda.Body.NodeType == ExpressionType.Conditional && sep == " ")
            {
                var c = lambda.Body as ConditionalExpression;

                var r = VisitConditional(c);
                if (r is not PartialSqlString)
                    return r;

                return $"{r}={GetQuotedTrueValue()}";
            }

            return Visit(lambda.Body);
        }

        public virtual object GetValue(object value, Type type)
        {
            if (skipParameterizationForThisExpression)
                return DialectProvider.GetQuotedValue(value, type);

            var paramValue = DialectProvider.GetParamValue(value, type);
            return paramValue ?? "null";
        }

        protected virtual object VisitBinary(BinaryExpression b)
        {
            object originalLeft = null, originalRight = null, left, right;
            var operand = BindOperant(b.NodeType);   //sep= " " ??
            if (operand is "AND" or "OR")
            {
                if (IsBooleanComparison(b.Left))
                {
                    left = VisitMemberAccess((MemberExpression) b.Left);
                    if (left is PartialSqlString)
                        left = new PartialSqlString($"{left}={GetQuotedTrueValue()}");
                }
                else if (b.Left is ConditionalExpression expr)
                {
                    left = VisitConditional(expr);
                    if (left is PartialSqlString)
                        left = new PartialSqlString($"{left}={GetQuotedTrueValue()}");
                }
                else left = Visit(b.Left);

                if (IsBooleanComparison(b.Right))
                {
                    right = VisitMemberAccess((MemberExpression) b.Right);
                    if (right is PartialSqlString)
                        right = new PartialSqlString($"{right}={GetQuotedTrueValue()}");
                }
                else if (b.Right is ConditionalExpression expr)
                {
                    right = VisitConditional(expr);
                    if (right is PartialSqlString)
                        right = new PartialSqlString($"{right}={GetQuotedTrueValue()}");
                }
                else right = Visit(b.Right);

                if (left is not PartialSqlString && right is not PartialSqlString)
                {
                    var result = CachedExpressionCompiler.Evaluate(PreEvaluateBinary(b, left, right));
                    return result;
                }

                if (left is not PartialSqlString)
                    left = ((bool)left) ? GetTrueExpression() : GetFalseExpression();
                if (right is not PartialSqlString)
                    right = ((bool)right) ? GetTrueExpression() : GetFalseExpression();
            }
            else if ((operand is "=" or "<>") && b.Left is MethodCallExpression methodExpr && methodExpr.Method.Name == "CompareString")
            {
                //Handle VB.NET converting (x => x.Name == "Foo") into (x => CompareString(x.Name, "Foo", False)
                var args = this.VisitExpressionList(methodExpr.Arguments);
                right = GetValue(args[1], typeof(string));
                ConvertToPlaceholderAndParameter(ref right);
                return new PartialSqlString($"({args[0]} {operand} {right})");
            }
            else
            {
                originalLeft = left = Visit(b.Left);
                originalRight = right = Visit(b.Right);

                // Handle "expr = true/false", including with the constant on the left
                if (operand is "=" or "<>")
                {
                    if (left is bool)
                    {
                        Swap(ref left, ref right); // Should be safe to swap for equality/inequality checks
                    }

                    if (right is bool &&
                        (left == null || left.ToString().Equals("null", StringComparison.OrdinalIgnoreCase)))
                    {
                        if (operand == "=")
                            return false; // "null == true/false" becomes "false"
                        if (operand == "<>")
                            return true; // "null != true/false" becomes "true"
                    }

                    // Don't change anything when "expr" is a column name or ConditionalExpression - then we really want "ColName = 1" or (Case When 1=0 Then 1 Else 0 End = 1)
                    if (right is bool rightBool && !IsFieldName(left) && b.Left is not ConditionalExpression) 
                    {
                        if (operand == "=")
                            return rightBool ? left : GetNotValue(left); // "expr == true" becomes "expr", "expr == false" becomes "not (expr)"
                        if (operand == "<>")
                            return rightBool ? GetNotValue(left) : left; // "expr != true" becomes "not (expr)", "expr != false" becomes "expr"
                    }
                }

                var leftEnum = left as EnumMemberAccess;
                //The real type should be read when a non-direct member is accessed. For example Sql.TableAlias(x.State, "p"),alias conversion should be performed when "x.State" is an enum 
                if (leftEnum == null && left is PartialSqlString pss && pss.EnumMember != null) leftEnum = pss.EnumMember;

                var rightEnum = right as EnumMemberAccess;

                var rightNeedsCoercing = leftEnum != null && rightEnum == null;
                var leftNeedsCoercing = rightEnum != null && leftEnum == null;

                if (rightNeedsCoercing)
                {
                    var rightPartialSql = right as PartialSqlString;
                    if (rightPartialSql == null)
                    {
                        right = GetValue(right, leftEnum.EnumType);
                    }
                }
                else if (leftNeedsCoercing)
                {
                    if (left is not PartialSqlString)
                    {
                        left = DialectProvider.GetQuotedValue(left, rightEnum.EnumType);
                    }
                }
                else if (left is not PartialSqlString && right is not PartialSqlString)
                {
                    var evaluatedValue = CachedExpressionCompiler.Evaluate(PreEvaluateBinary(b, left, right));
                    var result = VisitConstant(Expression.Constant(evaluatedValue));
                    return result;
                }
                else if (left is not PartialSqlString)
                {
                    left = DialectProvider.GetQuotedValue(left, left?.GetType());
                }
                else if (right is not PartialSqlString)
                {
                    right = GetValue(right, right?.GetType());
                }
            }

            if (left.ToString().Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                Swap(ref left, ref right); // "null is x" will not work, so swap the operands
            }

            var separator = sep;
            if (right.ToString().Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                if (operand == "=")
                    operand = "is";
                else if (operand == "<>")
                    operand = "is not";

                separator = " ";
            }

            if (operand == "+" && b.Left.Type == typeof(string) && b.Right.Type == typeof(string))
                return BuildConcatExpression(new List<object> {left, right});

            VisitFilter(operand, originalLeft, originalRight, ref left, ref right);

            switch (operand)
            {
                case "MOD":
                case "COALESCE":
                    return new PartialSqlString($"{operand}({left},{right})");
                default:
                    return new PartialSqlString("(" + left + separator + operand + separator + right + ")");
            }
        }

        private BinaryExpression PreEvaluateBinary(BinaryExpression b, object left, object right)
        {
            var visitedBinaryExp = b;

            if (IsParameterAccess(b.Left) || IsParameterAccess(b.Right))
            {
                var eLeft = !IsParameterAccess(b.Left) ? b.Left : Expression.Constant(left, b.Left.Type);
                var eRight = !IsParameterAccess(b.Right) ? b.Right : Expression.Constant(right, b.Right.Type);
                if (b.NodeType == ExpressionType.Coalesce)
                    visitedBinaryExp = Expression.Coalesce(eLeft, eRight, b.Conversion);
                else
                    visitedBinaryExp = Expression.MakeBinary(b.NodeType, eLeft, eRight, b.IsLiftedToNull, b.Method);
            }

            return visitedBinaryExp;
        }

        /// <summary>
        /// Determines whether the expression is the parameter inside MemberExpression which should be compared with TrueExpression.
        /// </summary>
        /// <returns>Returns true if the specified expression is the parameter inside MemberExpression which should be compared with TrueExpression;
        /// otherwise, false.</returns>
        protected virtual bool IsBooleanComparison(Expression e)
        {
            if (e is not MemberExpression m) 
                return false;

            if (m.Member.DeclaringType.IsNullableType() &&
                m.Member.Name == "HasValue") //nameof(Nullable<bool>.HasValue)
                return false;

            return IsParameterAccess(m);
        }

        /// <summary>
        /// Determines whether the expression is the parameter.
        /// </summary>
        /// <returns>Returns true if the specified expression is parameter;
        /// otherwise, false.</returns>
        protected virtual bool IsParameterAccess(Expression e)
        {
            return CheckExpressionForTypes(e, new[] { ExpressionType.Parameter });
        }

        /// <summary>
        /// Determines whether the expression is a Parameter or Convert Expression.
        /// </summary>
        /// <returns>Returns true if the specified expression is parameter or convert;
        /// otherwise, false.</returns>
        protected virtual bool IsParameterOrConvertAccess(Expression e)
        {
            return CheckExpressionForTypes(e, new[] { ExpressionType.Parameter, ExpressionType.Convert });
        }

        /// <summary>
        /// Check whether the expression is a constant expression to determine
        /// whether we should use the expression value instead of Column Name 
        /// </summary>
        protected virtual bool IsConstantExpression(Expression e)
        {
            return CheckExpressionForTypes(e, new[] { ExpressionType.Constant });
        }

        protected bool CheckExpressionForTypes(Expression e, ExpressionType[] types)
        {
            while (e != null)
            {
                if (types.Contains(e.NodeType))
                {
                    var subUnaryExpr = e as UnaryExpression;
                    var isSubExprAccess = subUnaryExpr?.Operand is IndexExpression;
                    if (!isSubExprAccess)
                        return true;
                }

                if (e is BinaryExpression binaryExpr)
                {
                    if (CheckExpressionForTypes(binaryExpr.Left, types))
                        return true;

                    if (CheckExpressionForTypes(binaryExpr.Right, types))
                        return true;
                }

                if (e is MethodCallExpression methodCallExpr)
                {
                    for (var i = 0; i < methodCallExpr.Arguments.Count; i++)
                    {
                        if (CheckExpressionForTypes(methodCallExpr.Arguments[i], types))
                            return true;
                    }

                    if (CheckExpressionForTypes(methodCallExpr.Object, types))
                        return true;
                }

                if (e is UnaryExpression unaryExpr)
                {
                    if (CheckExpressionForTypes(unaryExpr.Operand, types))
                        return true;
                }

                if (e is ConditionalExpression condExpr)
                {
                    if (CheckExpressionForTypes(condExpr.Test, types))
                        return true;

                    if (CheckExpressionForTypes(condExpr.IfTrue, types))
                        return true;

                    if (CheckExpressionForTypes(condExpr.IfFalse, types))
                        return true;
                }

                var memberExpr = e as MemberExpression;
                e = memberExpr?.Expression;
            }

            return false;
        }

        private static void Swap(ref object left, ref object right)
        {
            var temp = right;
            right = left;
            left = temp;
        }

        protected virtual void VisitFilter(string operand, object originalLeft, object originalRight, ref object left, ref object right)
        {
            if (skipParameterizationForThisExpression || visitedExpressionIsTableColumn)
                return;

            if (originalLeft is EnumMemberAccess && originalRight is EnumMemberAccess)
                return;

            if (operand == "AND" || operand == "OR" || operand == "is" || operand == "is not")
                return;

            if (!(right is PartialSqlString))
            {
                ConvertToPlaceholderAndParameter(ref right);
            }
        }

        protected virtual void ConvertToPlaceholderAndParameter(ref object right)
        {
            var parameter = AddParam(right);

            right = parameter.ParameterName;
        }

        protected virtual object VisitMemberAccess(MemberExpression m)
        {
            if (m.Expression != null)
            {
                if (m.Member.DeclaringType.IsNullableType())
                {
                    if (m.Member.Name == nameof(Nullable<bool>.Value))
                        return Visit(m.Expression);
                    if (m.Member.Name == nameof(Nullable<bool>.HasValue))
                    {
                        var doesNotEqualNull = Expression.MakeBinary(ExpressionType.NotEqual, m.Expression, Expression.Constant(null));
                        return Visit(doesNotEqualNull); // Nullable<T>.HasValue is equivalent to "!= null"
                    }

                    throw new ArgumentException($"Expression '{m}' accesses unsupported property '{m.Member}' of Nullable<T>");
                }

                if (m.Member.DeclaringType == typeof(string) &&
                    m.Member.Name == nameof(string.Length))
                {
                    return VisitLengthStringProperty(m);
                }

                if (IsParameterOrConvertAccess(m))
                {
                    // We don't want to use the Column Name for constant expressions unless we're in a Sql. method call
                    if (inSqlMethodCall || !IsConstantExpression(m))
                        return GetMemberExpression(m);
                }
            }

            return CachedExpressionCompiler.Evaluate(m);
        }

        protected virtual object GetMemberExpression(MemberExpression m)
        {
            var propertyInfo = m.Member as PropertyInfo;

            var modelType = m.Expression.Type;
            if (m.Expression.NodeType == ExpressionType.Convert)
            {
                if (m.Expression is UnaryExpression unaryExpr)
                {
                    modelType = unaryExpr.Operand.Type;
                }
            }

            OnVisitMemberType(modelType);

            var tableDef = modelType.GetModelDefinition();

            var tableAlias = GetTableAlias(m);
            var columnName = tableAlias == null
                ? GetQuotedColumnName(tableDef, m.Member.Name)
                : GetQuotedColumnName(tableDef, tableAlias, m.Member.Name);

            if (propertyInfo != null && propertyInfo.PropertyType.IsEnum)
                return new EnumMemberAccess(columnName, propertyInfo.PropertyType);
            
            return new PartialSqlString(columnName);
        }

        protected virtual string GetTableAlias(MemberExpression m)
        {
            if (originalLambda == null)
                return null;

            if (m.Expression is ParameterExpression pe)
            {
                var tableParamName = originalLambda.Parameters.Count > 0 && originalLambda.Parameters[0].Type == ModelDef.ModelType
                    ? originalLambda.Parameters[0].Name
                    : null;

                if (pe.Type == ModelDef.ModelType && pe.Name == tableParamName)
                    return TableAlias;

                var joinType = joinAlias?.ModelDef?.ModelType;
                var joinParamName = originalLambda.Parameters.Count > 1 && originalLambda.Parameters[1].Type == joinType
                    ? originalLambda.Parameters[1].Name
                    : null;

                if (pe.Type == joinType && pe.Name == joinParamName)
                    return joinAlias.Alias;
            }

            return null;
        }

        protected virtual void OnVisitMemberType(Type modelType)
        {
            var tableDef = modelType.GetModelDefinition();
            if (tableDef != null)
                visitedExpressionIsTableColumn = true;
        }

        protected virtual object VisitMemberInit(MemberInitExpression exp)
        {
            return CachedExpressionCompiler.Evaluate(exp);
        }

        protected virtual object VisitNew(NewExpression nex)
        {
            var isAnonType = nex.Type.Name.StartsWith("<>");
            if (isAnonType)
            {
                var exprs = VisitExpressionList(nex.Arguments);
                if (isSelectExpression)
                {
                    for (var i = 0; i < exprs.Count; ++i)
                    {
                        exprs[i] = SetAnonTypePropertyNamesForSelectExpression(exprs[i], nex.Arguments[i], nex.Members[i]);
                    }
                }

                return new SelectList(exprs);
            }

            return CachedExpressionCompiler.Evaluate(nex);
        }

        bool IsLambdaArg(Expression expr)
        {
            if (expr is ParameterExpression pe)
                return IsLambdaArg(pe.Name);
            if (expr is UnaryExpression ue && ue.Operand is ParameterExpression uepe)
                return IsLambdaArg(uepe.Name);
            return false;
        }

        bool IsLambdaArg(string name)
        {
            var args = originalLambda?.Parameters;
            if (args != null)
            {
                foreach (var x in args)
                {
                    if (x.Name == name)
                        return true;
                }
            }
            return false;
        }

        private object SetAnonTypePropertyNamesForSelectExpression(object expr, Expression arg, MemberInfo member)
        {
            // When selecting a column use the anon type property name, rather than the table property name, as the returned column name
            if (arg is MemberExpression propExpr && IsLambdaArg(propExpr.Expression))
            {
                if (UseSelectPropertiesAsAliases ||                  // Use anon property alias when explicitly requested
                    propExpr.Member.Name != member.Name ||           // or when names don't match 
                    propExpr.Expression.Type != ModelDef.ModelType || // or when selecting a field from a different table
                    member.Name != ModelDef.FieldDefinitions.First(x => x.Name == member.Name).FieldName)  //or when name and alias don't match  
                    return new SelectItemExpression(DialectProvider, expr.ToString(), member.Name);

                return expr;
            }

            // When selecting an entire table use the anon type property name as a prefix for the returned column name
            // to allow the caller to distinguish properties with the same names from different tables

            var selectList = arg is ParameterExpression paramExpr && paramExpr.Name != member.Name
                ? expr as SelectList
                : null;
            if (selectList != null)
            {
                foreach (var item in selectList.Items)
                {
                    if (item is SelectItem selectItem)
                    {
                        if (!string.IsNullOrEmpty(selectItem.Alias))
                        {
                            selectItem.Alias = member.Name + selectItem.Alias;
                        }
                        else
                        {
                            if (item is SelectItemColumn columnItem)
                            {
                                columnItem.Alias = member.Name + columnItem.ColumnName;
                            }
                        }
                    }
                }
            }

            var methodCallExpr = arg as MethodCallExpression;
            var mi = methodCallExpr?.Method;
            var declareType = mi?.DeclaringType;
            if (declareType != null && declareType.Name == nameof(Sql))
            {
                if (mi.Name == nameof(Sql.TableAlias) || mi.Name == nameof(Sql.JoinAlias))
                {
                    if (expr is PartialSqlString ps && ps.Text.IndexOf(',') >= 0)
                        return ps;                                               // new { buyer = Sql.TableAlias(b, "buyer")
                    return new PartialSqlString(expr + " AS " + member.Name);    // new { BuyerName = Sql.TableAlias(b.Name, "buyer") }
                }
                
                if (mi.Name != nameof(Sql.Desc) && mi.Name != nameof(Sql.Asc) && mi.Name != nameof(Sql.As) && mi.Name != nameof(Sql.AllFields))
                    return new PartialSqlString(expr + " AS " + member.Name);    // new { Alias = Sql.Count("*") }
            }

            if (expr is string s && s == Sql.EOT) // new { t1 = Sql.EOT, t2 = "0 EOT" }
                return new PartialSqlString(s);

            if (arg is ConditionalExpression ce ||                           // new { Alias = x.Value > 1 ? 1 : x.Value }
                arg is BinaryExpression      be ||                           // new { Alias = x.First + " " + x.Last }
                arg is MemberExpression      me ||                           // new { Alias = DateTime.UtcNow }
                arg is ConstantExpression ct)                                // new { Alias = 1 }
            {
                IOrmLiteConverter converter;
                var strExpr = !(expr is PartialSqlString) && (converter = DialectProvider.GetConverterBestMatch(expr.GetType())) != null
                    ? converter.ToQuotedString(expr.GetType(), expr)
                    : expr.ToString();
                
                return new PartialSqlString(OrmLiteUtils.UnquotedColumnName(strExpr) != member.Name
                    ? strExpr + " AS " + member.Name 
                    : strExpr);
            }

            var usePropertyAlias = UseSelectPropertiesAsAliases ||
                (expr is PartialSqlString p && Equals(p, PartialSqlString.Null)); // new { Alias = (DateTime?)null }
            return usePropertyAlias
                ? new SelectItemExpression(DialectProvider, expr.ToString(), member.Name)
                : expr;
        }

        private static void StripAliases(SelectList selectList)
        {
            if (selectList == null)
                return;

            foreach (var item in selectList.Items)
            {
                if (item is SelectItem selectItem)
                {
                    selectItem.Alias = null;
                }
                else if (item is PartialSqlString p)
                {
                    if (p.Text.IndexOf(' ') >= 0)
                    {
                        var right = p.Text.RightPart(' ');
                        if (right.StartsWithIgnoreCase("AS "))
                        {
                            p.Text = p.Text.LeftPart(' ');
                        }
                    }
                }
            }
        }

        private class SelectList
        {
            public readonly IEnumerable<object> Items;

            public SelectList(IEnumerable<object> items)
            {
                this.Items = items;
            }

            public override string ToString()
            {
                return Items.ToSelectString();
            }
        }

        protected virtual object VisitParameter(ParameterExpression p)
        {
            var paramModelDef = p.Type.GetModelDefinition();
            if (paramModelDef != null)
            {
                var tablePrefix = paramModelDef == ModelDef && TableAlias != null
                    ? TableAlias
                    : paramModelDef.ModelName;
                return new SelectList(DialectProvider.GetColumnNames(paramModelDef, tablePrefix));
            }

            return p.Name;
        }

        protected virtual object VisitConstant(ConstantExpression c)
        {
            if (c.Value == null)
                return PartialSqlString.Null;

            return c.Value;
        }

        protected virtual object VisitUnary(UnaryExpression u)
        {
            switch (u.NodeType)
            {
                case ExpressionType.Not:
                    var o = Visit(u.Operand);
                    return GetNotValue(o);
                case ExpressionType.Convert:
                    if (u.Method != null)
                    {
                        var e = u.Operand;
                        if (IsParameterAccess(e))
                            return Visit(e);

                        return CachedExpressionCompiler.Evaluate(u);
                    }
                    break;
            }
            return Visit(u.Operand);
        }

        protected virtual object VisitIndexExpression(IndexExpression e)
        {
            var arg = e.Arguments[0];
            var oIndex = arg is ConstantExpression constant
                ? constant.Value
                : CachedExpressionCompiler.Evaluate(arg);

            var index = (int)Convert.ChangeType(oIndex, typeof(int));
            var oCollection = CachedExpressionCompiler.Evaluate(e.Object);

            if (oCollection is List<object> list)
                return list[index];

            throw new NotImplementedException("Unknown Expression: " + e);
        }

        protected virtual object VisitConditional(ConditionalExpression e)
        {
            var test = IsBooleanComparison(e.Test)
                ? new PartialSqlString($"{VisitMemberAccess((MemberExpression) e.Test)}={GetQuotedTrueValue()}")
                : Visit(e.Test);

            if (test is bool)
            {
                if ((bool) test)
                {
                    var ifTrue = Visit(e.IfTrue);
                    if (!IsSqlClass(ifTrue))
                    {
                        if (sep == " ")
                            ifTrue = new PartialSqlString(ConvertToParam(ifTrue));
                    }
                    else if (e.IfTrue.Type == typeof(bool))
                    {
                        var isBooleanComparison = IsBooleanComparison(e.IfTrue);
                        if (!isBooleanComparison)
                        {
                            if (sep == " ")
                                ifTrue = ifTrue.ToString();
                            else
                                ifTrue = new PartialSqlString($"(CASE WHEN {ifTrue} THEN {1} ELSE {0} END)");
                        }
                    }

                    return ifTrue;
                }

                var ifFalse = Visit(e.IfFalse);
                if (!IsSqlClass(ifFalse))
                {
                    if (sep == " ")
                        ifFalse = new PartialSqlString(ConvertToParam(ifFalse));
                }
                else if (e.IfFalse.Type == typeof(bool))
                {
                    var isBooleanComparison = IsBooleanComparison(e.IfFalse);
                    if (!isBooleanComparison)
                    {
                        if (sep == " ")
                            ifFalse = ifFalse.ToString();
                        else
                            ifFalse = new PartialSqlString($"(CASE WHEN {ifFalse} THEN {1} ELSE {0} END)");
                    }
                }

                return ifFalse;
            }
            else
            {
                var ifTrue = Visit(e.IfTrue);
                if (!IsSqlClass(ifTrue))
                    ifTrue = ConvertToParam(ifTrue);
                else if (e.IfTrue.Type == typeof(bool))
                {
                    var isBooleanComparison = IsBooleanComparison(e.IfTrue);
                    if (!isBooleanComparison)
                    {
                        ifTrue = $"(CASE WHEN {ifTrue} THEN {GetQuotedTrueValue()} ELSE {GetQuotedFalseValue()} END)";
                    }
                }

                var ifFalse = Visit(e.IfFalse);
                if (!IsSqlClass(ifFalse))
                    ifFalse = ConvertToParam(ifFalse);
                else if (e.IfFalse.Type == typeof(bool))
                {
                    var isBooleanComparison = IsBooleanComparison(e.IfFalse);
                    if (!isBooleanComparison)
                    {
                        ifFalse = $"(CASE WHEN {ifFalse} THEN {GetQuotedTrueValue()} ELSE {GetQuotedFalseValue()} END)";
                    }
                }

                return new PartialSqlString($"(CASE WHEN {test} THEN {ifTrue} ELSE {ifFalse} END)");
            }
        }

        private object GetNotValue(object o)
        {
            if (!(o is PartialSqlString))
                return !(bool) o;

            if (IsFieldName(o))
                return new PartialSqlString(o + "=" + GetQuotedFalseValue());

            return new PartialSqlString("NOT (" + o + ")");
        }

        protected virtual bool IsColumnAccess(MethodCallExpression m)
        {
            if (m.Object == null)
            {
                foreach (var arg in m.Arguments)
                {
                    if (!(arg is LambdaExpression) &&
                        IsParameterAccess(arg))
                    {
                        return true;
                    }
                }

                return false;
            }

            if (m.Object is MethodCallExpression methCallExp)
                return IsColumnAccess(methCallExp);

            if (m.Object is ConditionalExpression condExp)
                return IsParameterAccess(condExp);

            if (m.Object is UnaryExpression unaryExp)
                return IsParameterAccess(unaryExp);

            var exp = m.Object as MemberExpression;
            return IsParameterAccess(exp)
                   && IsJoinedTable(exp.Expression.Type);
        }


        protected virtual object VisitMethodCall(MethodCallExpression m)
        {
            if (m.Method.DeclaringType == typeof(Sql))
            {
                var hold = inSqlMethodCall;
                inSqlMethodCall = true;
                var ret = VisitSqlMethodCall(m);
                inSqlMethodCall = hold;
                return ret;
            }

            if (IsStaticArrayMethod(m))
                return VisitStaticArrayMethodCall(m);

            if (IsEnumerableMethod(m))
                return VisitEnumerableMethodCall(m);

            if (IsStaticStringMethod(m))
                return VisitStaticStringMethodCall(m);

            if (IsColumnAccess(m))
                return VisitColumnAccessMethod(m);

            return EvaluateExpression(m);
        }

        private object EvaluateExpression(Expression m)
        {
            try
            {
                return CachedExpressionCompiler.Evaluate(m);
            }
            catch (InvalidOperationException)
            {
                if (originalLambda == null)
                    throw;
                    
                // Can't use expression.Compile() if lambda expression contains captured parameters.
                // Fallback invokes expression with default parameters from original lambda expression  
                
                var lambda = Expression.Lambda(m, originalLambda.Parameters).Compile();

                var exprParams = new object[originalLambda.Parameters.Count];
                for (var i = 0; i < originalLambda.Parameters.Count; i++)
                {
                    var p = originalLambda.Parameters[i];
                    exprParams[i] = p.Type.CreateInstance();
                }

                var ret = lambda.DynamicInvoke(exprParams);
                return ret;
            }
        }

        protected virtual List<object> VisitExpressionList(ReadOnlyCollection<Expression> original)
        {
            var list = new List<object>();
            for (int i = 0, n = original.Count; i < n; i++)
            {
                var e = original[i];
                if (e.NodeType == ExpressionType.NewArrayInit ||
                    e.NodeType == ExpressionType.NewArrayBounds)
                {
                    list.AddRange(VisitNewArrayFromExpressionList(e as NewArrayExpression));
                }
                else
                {
                    list.Add(Visit(e));
                }
            }
            return list;
        }

        protected virtual List<object> VisitInSqlExpressionList(ReadOnlyCollection<Expression> original)
        {
            var list = new List<object>();
            for (int i = 0, n = original.Count; i < n; i++)
            {
                var e = original[i];
                if (e.NodeType == ExpressionType.NewArrayInit ||
                    e.NodeType == ExpressionType.NewArrayBounds)
                {
                    list.AddRange(VisitNewArrayFromExpressionList(e as NewArrayExpression));
                }
                else if (e.NodeType == ExpressionType.MemberAccess)
                {
                    list.Add(VisitMemberAccess(e as MemberExpression));
                }
                else
                {
                    list.Add(Visit(e));
                }
            }
            return list;
        }

        protected virtual object VisitNewArray(NewArrayExpression na)
        {
            var exprs = VisitExpressionList(na.Expressions);
            var sb = StringBuilderCache.Allocate();
            foreach (var e in exprs)
            {
                sb.Append(sb.Length > 0 ? "," + e : e);
            }
            return StringBuilderCache.ReturnAndFree(sb);
        }

        protected virtual List<object> VisitNewArrayFromExpressionList(NewArrayExpression na)
        {
            var exprs = VisitExpressionList(na.Expressions);
            return exprs;
        }

        protected virtual string BindOperant(ExpressionType e)
        {
            switch (e)
            {
                case ExpressionType.Equal:
                    return "=";
                case ExpressionType.NotEqual:
                    return "<>";
                case ExpressionType.GreaterThan:
                    return ">";
                case ExpressionType.GreaterThanOrEqual:
                    return ">=";
                case ExpressionType.LessThan:
                    return "<";
                case ExpressionType.LessThanOrEqual:
                    return "<=";
                case ExpressionType.AndAlso:
                    return "AND";
                case ExpressionType.OrElse:
                    return "OR";
                case ExpressionType.Add:
                    return "+";
                case ExpressionType.Subtract:
                    return "-";
                case ExpressionType.Multiply:
                    return "*";
                case ExpressionType.Divide:
                    return "/";
                case ExpressionType.Modulo:
                    return "MOD";
                case ExpressionType.Coalesce:
                    return "COALESCE";
                case ExpressionType.And:
                    return "&";
                case ExpressionType.Or:
                    return "|";
                case ExpressionType.ExclusiveOr:
                    return "^";
                case ExpressionType.LeftShift:
                    return "<<";
                case ExpressionType.RightShift:
                    return ">>";
                default:
                    return e.ToString();
            }
        }
        
        protected virtual string GetQuotedColumnName(ModelDefinition tableDef, string memberName) => // Always call if no tableAlias to exec overrides  
            GetQuotedColumnName(tableDef, null, memberName);
        
        protected virtual string GetQuotedColumnName(ModelDefinition tableDef, string tableAlias, string memberName)
        {
            if (useFieldName)
            {
                var fd = tableDef.FieldDefinitions.FirstOrDefault(x => x.Name == memberName);
                var fieldName = fd != null
                    ? fd.FieldName
                    : memberName;

                if (tableDef.ModelType.IsInterface && this.ModelDef.ModelType.HasInterface(tableDef.ModelType))
                {
                    tableDef = this.ModelDef;
                }

                if (fd?.CustomSelect != null)
                    return fd.CustomSelect;

                var includePrefix = PrefixFieldWithTableName && !tableDef.ModelType.IsInterface;
                return includePrefix
                    ? (tableAlias == null 
                        ? DialectProvider.GetQuotedColumnName(tableDef, fieldName) 
                        : DialectProvider.GetQuotedColumnName(tableDef, tableAlias, fieldName))
                    : DialectProvider.GetQuotedColumnName(fieldName);
            }
            return memberName;
        }

        protected string RemoveQuoteFromAlias(string exp)
        {
            if ((exp.StartsWith("\"") || exp.StartsWith("`") || exp.StartsWith("'"))
                &&
                (exp.EndsWith("\"") || exp.EndsWith("`") || exp.EndsWith("'")))
            {
                exp = exp.Remove(0, 1);
                exp = exp.Remove(exp.Length - 1, 1);
            }
            return exp;
        }

        protected virtual bool IsFieldName(object quotedExp)
        {
            var fieldExpr = quotedExp.ToString().StripTablePrefixes();
            var unquotedExpr = fieldExpr.StripDbQuotes();

            var isTableField = modelDef.FieldDefinitionsArray
                .Any(x => GetColumnName(x.FieldName) == unquotedExpr);
            if (isTableField)
                return true;

            var isJoinedField = tableDefs.Any(t => t.FieldDefinitionsArray
                .Any(x => GetColumnName(x.FieldName) == unquotedExpr));

            return isJoinedField;
        }

        protected string GetColumnName(string fieldName)
        {
            return DialectProvider.NamingStrategy.GetColumnName(fieldName);
        }

        protected object GetTrueExpression()
        {
            return new PartialSqlString($"({GetQuotedTrueValue()}={GetQuotedTrueValue()})");
        }
        
        protected object GetFalseExpression()
        {
            return new PartialSqlString($"({GetQuotedTrueValue()}={GetQuotedFalseValue()})");
        }

        private string quotedTrue;
        protected object GetQuotedTrueValue()
        {
            return new PartialSqlString(quotedTrue ??= DialectProvider.GetQuotedValue(true, typeof(bool)));
        }

        private string quotedFalse;
        protected object GetQuotedFalseValue()
        {
            return new PartialSqlString(quotedFalse ??= DialectProvider.GetQuotedValue(false, typeof(bool)));
        }

        private void BuildSelectExpression(string fields, bool distinct)
        {
            OnlyFields = null;
            selectDistinct = distinct;
            
            selectExpression = $"SELECT {(selectDistinct ? "DISTINCT " : "")}" +
               (string.IsNullOrEmpty(fields) 
                    ? DialectProvider.GetColumnNames(modelDef, PrefixFieldWithTableName ? TableAlias ?? ModelDef.ModelName : null).ToSelectString() 
                    : fields);
        }

        public IList<string> GetAllFields()
        {
            return modelDef.FieldDefinitions.ConvertAll(r => r.Name);
        }

        protected virtual bool IsStaticArrayMethod(MethodCallExpression m)
        {
            return (m.Object == null 
                && m.Method.Name == "Contains"
                && m.Arguments.Count == 2);
        }

        protected virtual object VisitStaticArrayMethodCall(MethodCallExpression m)
        {
            switch (m.Method.Name)
            {
                case "Contains":
                    List<object> args = this.VisitExpressionList(m.Arguments);
                    object quotedColName = args.Last();

                    Expression memberExpr = m.Arguments[0];
                    if (memberExpr.NodeType == ExpressionType.MemberAccess)
                        memberExpr = m.Arguments[0] as MemberExpression;

                    return ToInPartialString(memberExpr, quotedColName);

                default:
                    throw new NotSupportedException();
            }
        }

        private static bool IsEnumerableMethod(MethodCallExpression m)
        {
            return m.Object != null
                && m.Object.Type.IsOrHasGenericInterfaceTypeOf(typeof(IEnumerable<>))
                && m.Object.Type != typeof(string)
                && m.Method.Name == "Contains"
                && m.Arguments.Count == 1;
        }

        protected virtual object VisitEnumerableMethodCall(MethodCallExpression m)
        {
            switch (m.Method.Name)
            {
                case "Contains":
                    List<object> args = this.VisitExpressionList(m.Arguments);
                    object quotedColName = args[0];
                    return ToInPartialString(m.Object, quotedColName);

                default:
                    throw new NotSupportedException();
            }
        }

        private object ToInPartialString(Expression memberExpr, object quotedColName)
        {
            var result = EvaluateExpression(memberExpr);

            var inArgs = Sql.Flatten(result as IEnumerable);

            var sqlIn = inArgs.Count > 0
                ? CreateInParamSql(inArgs)
                : "NULL";

            var statement = $"{quotedColName} IN ({sqlIn})";
            return new PartialSqlString(statement);
        }

        protected virtual bool IsStaticStringMethod(MethodCallExpression m)
        {
            return (m.Object == null
                    && (m.Method.Name == nameof(String.Concat) || m.Method.Name == nameof(String.Compare)));
        }

        protected virtual object VisitStaticStringMethodCall(MethodCallExpression m)
        {
            switch (m.Method.Name)
            {
                case nameof(String.Concat):
                    return BuildConcatExpression(VisitExpressionList(m.Arguments));
                case nameof(String.Compare):
                    return BuildCompareExpression(VisitExpressionList(m.Arguments));

                default:
                    throw new NotSupportedException();
            }
        }

        private object VisitLengthStringProperty(MemberExpression m)
        {
            var sql = Visit(m.Expression);
            if (!IsSqlClass(sql))
            {
                if (sql == null)
                    return 0;

                sql = ((string) sql).Length;
                return sql;
            }

            return ToLengthPartialString(sql);
        }

        protected virtual PartialSqlString ToLengthPartialString(object arg)
        {
            return new PartialSqlString($"CHAR_LENGTH({arg})");
        }

        private PartialSqlString BuildConcatExpression(List<object> args)
        {
            for (int i = 0; i < args.Count; i++)
            {
                if (!(args[i] is PartialSqlString))
                    args[i] = ConvertToParam(args[i]);
            }
            return ToConcatPartialString(args);
        }

        private PartialSqlString BuildCompareExpression(List<object> args)
        {
            for (int i = 0; i < args.Count; i++)
            {
                if (!(args[i] is PartialSqlString))
                    args[i] = ConvertToParam(args[i]);
            }
            return ToComparePartialString(args);
        }

        protected PartialSqlString ToConcatPartialString(List<object> args)
        {
            return new PartialSqlString(DialectProvider.SqlConcat(args));
        }

        protected virtual PartialSqlString ToComparePartialString(List<object> args)
        {
            return new PartialSqlString($"(CASE WHEN {args[0]} = {args[1]} THEN 0 WHEN {args[0]} > {args[1]} THEN 1 ELSE -1 END)");
        }

        protected virtual object VisitSqlMethodCall(MethodCallExpression m)
        {
            List<object> args = this.VisitInSqlExpressionList(m.Arguments);
            object quotedColName = args[0];
            var columnEnumMemberAccess = args[0] as EnumMemberAccess;
            args.RemoveAt(0);

            string statement;

            switch (m.Method.Name)
            {
                case nameof(Sql.In):
                    statement = ConvertInExpressionToSql(m, quotedColName);
                    break;
                case nameof(Sql.Asc):
                    statement = $"{quotedColName} ASC";
                    break;
                case nameof(Sql.Desc):
                    statement = $"{quotedColName} DESC";
                    break;
                case nameof(Sql.As):
                    statement = $"{quotedColName} AS {DialectProvider.GetQuotedColumnName(RemoveQuoteFromAlias(args[0].ToString()))}";
                    break;
                case nameof(Sql.Cast):
                    statement = DialectProvider.SqlCast(quotedColName, args[0].ToString());
                    break;
                case nameof(Sql.Sum):
                case nameof(Sql.Count):
                case nameof(Sql.Min):
                case nameof(Sql.Max):
                case nameof(Sql.Avg):
                    statement = $"{m.Method.Name}({quotedColName}{(args.Count == 1 ? $",{args[0]}" : "")})";
                    break;
                case nameof(Sql.CountDistinct):
                    statement = $"COUNT(DISTINCT {quotedColName})";
                    break;
                case nameof(Sql.AllFields):
                    var argDef = m.Arguments[0].Type.GetModelMetadata();
                    statement = DialectProvider.GetQuotedTableName(argDef) + ".*";
                    break;
                case nameof(Sql.JoinAlias):
                case nameof(Sql.TableAlias):
                    if (quotedColName is SelectList && m.Arguments.Count == 2 && m.Arguments[0] is ParameterExpression p)
                    {
                        var paramModelDef = p.Type.GetModelDefinition();
                        var alias = Visit(m.Arguments[1]).ToString();
                        statement = new SelectList(DialectProvider.GetColumnNames(paramModelDef, alias)).ToString();
                    }
                    else
                    {
                        statement = args[0] + "." + quotedColName.ToString().LastRightPart('.');
                    }
                    break;
                case nameof(Sql.Custom):
                    statement = quotedColName.ToString();
                    break;
                default:
                    throw new NotSupportedException();
            }

            return new PartialSqlString(statement, columnEnumMemberAccess);
        }

        protected string ConvertInExpressionToSql(MethodCallExpression m, object quotedColName)
        {
            var argValue = EvaluateExpression(m.Arguments[1]);

            if (argValue == null)
                return FalseLiteral; // "column IN (NULL)" is always false

            if (quotedColName is not PartialSqlString)
                quotedColName = ConvertToParam(quotedColName);

            if (argValue is IEnumerable enumerableArg)
            {
                var inArgs = Sql.Flatten(enumerableArg);
                if (inArgs.Count == 0)
                    return FalseLiteral; // "column IN ([])" is always false

                string sqlIn = CreateInParamSql(inArgs);
                return $"{quotedColName} IN ({sqlIn})";
            }

            if (argValue is ISqlExpression exprArg)
            {
                var subSelect = exprArg.ToSelectStatement(QueryType.Select);
                var renameParams = new List<Tuple<string,string>>();
                foreach (var p in exprArg.Params)
                {
                    var oldName = p.ParameterName;
                    var newName = DialectProvider.GetParam(Params.Count.ToString());
                    if (oldName != newName)
                    {
                        var pClone = DialectProvider.CreateParam().PopulateWith(p);
                        renameParams.Add(Tuple.Create(oldName, newName));
                        pClone.ParameterName = newName;
                        Params.Add(pClone);
                    }
                    else
                    {
                        Params.Add(p);
                    }
                }

                // regex replace doesn't work when param is at end of string "AND a = :0"
                var lastChar = subSelect[subSelect.Length - 1];
                if (!(char.IsWhiteSpace(lastChar) || lastChar == ')'))
                    subSelect += " ";
                
                for (var i = renameParams.Count - 1; i >= 0; i--)
                {
                    //Replace complete db params [@1] and not partial tokens [@1]0
                    var paramsRegex = new Regex(renameParams[i].Item1 + "([^\\d])");
                    subSelect = paramsRegex.Replace(subSelect, renameParams[i].Item2 + "$1");
                }
                
                return CreateInSubQuerySql(quotedColName, subSelect);
            }

            throw new NotSupportedException($"In({argValue.GetType()})");
        }

        protected virtual string CreateInSubQuerySql(object quotedColName, string subSelect)
        {
            return $"{quotedColName} IN ({subSelect})";
        }

        protected virtual object VisitColumnAccessMethod(MethodCallExpression m)
        {
            List<object> args = this.VisitExpressionList(m.Arguments);
            var quotedColName = Visit(m.Object);
            if (!IsSqlClass(quotedColName))
                quotedColName = ConvertToParam(quotedColName);

            var statement = "";

            var arg = args.Count > 0 ? args[0] : null;
            var wildcardArg = arg != null ? DialectProvider.EscapeWildcards(arg.ToString()) : "";
            var escapeSuffix = wildcardArg.IndexOf('^') >= 0 ? " escape '^'" : "";
            switch (m.Method.Name)
            {
                case "Trim":
                    statement = $"ltrim(rtrim({quotedColName}))";
                    break;
                case "LTrim":
                    statement = $"ltrim({quotedColName})";
                    break;
                case "RTrim":
                    statement = $"rtrim({quotedColName})";
                    break;
                case "ToUpper":
                    statement = $"upper({quotedColName})";
                    break;
                case "ToLower":
                    statement = $"lower({quotedColName})";
                    break;
                case "Equals":
                    var argType = arg?.GetType();
                    var converter = argType != null && argType != typeof(string) 
                        ? DialectProvider.GetConverterBestMatch(argType) 
                        : null;
                    statement = converter != null
                        ? $"{quotedColName}={ConvertToParam(converter.ToDbValue(argType, arg))}"
                        : $"{quotedColName}={ConvertToParam(arg)}";
                    break;                
                case "StartsWith":
                    statement = !OrmLiteConfig.StripUpperInLike
                        ? $"upper({quotedColName}) like {ConvertToParam(wildcardArg.ToUpper() + "%")}{escapeSuffix}"
                        : $"{quotedColName} like {ConvertToParam(wildcardArg + "%")}{escapeSuffix}";
                    break;
                case "EndsWith":
                    statement = !OrmLiteConfig.StripUpperInLike
                        ? $"upper({quotedColName}) like {ConvertToParam("%" + wildcardArg.ToUpper())}{escapeSuffix}"
                        : $"{quotedColName} like {ConvertToParam("%" + wildcardArg)}{escapeSuffix}";
                    break;
                case "Contains":
                    statement = !OrmLiteConfig.StripUpperInLike
                        ? $"upper({quotedColName}) like {ConvertToParam("%" + wildcardArg.ToUpper() + "%")}{escapeSuffix}"
                        : $"{quotedColName} like {ConvertToParam("%" + wildcardArg + "%")}{escapeSuffix}";
                    break;
                case "Substring":
                    var startIndex = int.Parse(args[0].ToString()) + 1;
                    statement = args.Count == 2 
                        ? GetSubstringSql(quotedColName, startIndex, int.Parse(args[1].ToString())) 
                        : GetSubstringSql(quotedColName, startIndex);
                    break;
                case "ToString":
                    statement = m.Object?.Type == typeof(string) 
                        ? $"({quotedColName})"
                        : ToCast(quotedColName.ToString());
                    break;
                default:
                    throw new NotSupportedException();
            }
            return new PartialSqlString(statement);
        }

        protected virtual string ToCast(string quotedColName)
        {
            return $"cast({quotedColName} as varchar(1000))";
        }

        public virtual string GetSubstringSql(object quotedColumn, int startIndex, int? length = null)
        {
            return length != null
                ? $"substring({quotedColumn} from {startIndex} for {length.Value})"
                : $"substring({quotedColumn} from {startIndex})";
        }

        public IDbDataParameter CreateParam(string name,
            object value = null,
            ParameterDirection direction = ParameterDirection.Input,
            DbType? dbType = null,
            DataRowVersion sourceVersion = DataRowVersion.Default)
        {
            var p = DialectProvider.CreateParam();
            p.ParameterName = DialectProvider.GetParam(name);
            p.Direction = direction;

            if (!DialectProvider.IsMySqlConnector()) //throws NotSupportedException
            {
                p.SourceVersion = sourceVersion;
            }

            DialectProvider.ConfigureParam(p, value, dbType);

            return p;
        }

        public IUntypedSqlExpression GetUntyped()
        {
            return new UntypedSqlExpressionProxy<T>(this);
        }
    }

    public interface ISqlExpression
    {
        List<IDbDataParameter> Params { get; }

        string ToSelectStatement();
        string ToSelectStatement(QueryType forType);
        string SelectInto<TModel>();
        string SelectInto<TModel>(QueryType forType);
    }

    public enum QueryType
    {
        Select,
        Single,
        Scalar,
    }

    public interface IHasDialectProvider
    {
        IOrmLiteDialectProvider DialectProvider { get; }
    }

    public class PartialSqlString
    {
        public static PartialSqlString Null = new("null");
        
        public PartialSqlString(string text) : this(text, null)
        {
        }

       public PartialSqlString(string text, EnumMemberAccess enumMember)
        {
            Text = text;
            EnumMember = enumMember;
        }

        public string Text { get; internal set; }
        public readonly EnumMemberAccess EnumMember;
        public override string ToString() => Text;

        protected bool Equals(PartialSqlString other) => Text == other.Text;
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PartialSqlString) obj);
        }
        public override int GetHashCode() => (Text != null ? Text.GetHashCode() : 0);
    }

    public class EnumMemberAccess : PartialSqlString
    {
        public EnumMemberAccess(string text, Type enumType)
            : base(text)
        {
            if (!enumType.IsEnum) throw new ArgumentException("Type not valid", nameof(enumType));

            EnumType = enumType;
        }

        public Type EnumType { get; private set; }
    }

    public abstract class SelectItem
    {
        protected SelectItem(IOrmLiteDialectProvider dialectProvider, string alias)
        {
            DialectProvider = dialectProvider ?? throw new ArgumentNullException(nameof(dialectProvider));

            Alias = alias;
        }

        /// <summary>
        /// Unquoted alias for the column or expression being selected.
        /// </summary>
        public string Alias { get; set; }

        protected IOrmLiteDialectProvider DialectProvider { get; set; }

        public abstract override string ToString();
    }

    public class SelectItemExpression : SelectItem
    {
        public SelectItemExpression(IOrmLiteDialectProvider dialectProvider, string selectExpression, string alias)
            : base(dialectProvider, alias)
        {
            if (string.IsNullOrEmpty(selectExpression))
                throw new ArgumentNullException(nameof(selectExpression));
            if (string.IsNullOrEmpty(alias))
                throw new ArgumentNullException(nameof(alias));

            SelectExpression = selectExpression;
            Alias = alias;
        }

        /// <summary>
        /// The SQL expression being selected, including any necessary quoting.
        /// </summary>
        public string SelectExpression { get; set; }

        public override string ToString()
        {
            var text = SelectExpression;
            if (!string.IsNullOrEmpty(Alias)) // Note that even though Alias must be non-empty in the constructor it may be set to null/empty later
                return text + " AS " + DialectProvider.GetQuotedName(Alias);

            return text;
        }
    }

    public class SelectItemColumn : SelectItem
    {
        public SelectItemColumn(IOrmLiteDialectProvider dialectProvider, string columnName, string columnAlias = null, string quotedTableAlias = null)
            : base(dialectProvider, columnAlias)
        {
            if (string.IsNullOrEmpty(columnName))
                throw new ArgumentNullException(nameof(columnName));

            ColumnName = columnName;
            QuotedTableAlias = quotedTableAlias;
        }

        /// <summary>
        /// Unquoted column name being selected.
        /// </summary>
        public string ColumnName { get; set; }
        /// <summary>
        /// Table name or alias used to prefix the column name, if any. Already quoted.
        /// </summary>
        public string QuotedTableAlias { get; set; }

        public override string ToString()
        {
            var text = DialectProvider.GetQuotedColumnName(ColumnName);

            if (!string.IsNullOrEmpty(QuotedTableAlias))
                text = QuotedTableAlias + "." + text;
            if (!string.IsNullOrEmpty(Alias))
                text += " AS " + DialectProvider.GetQuotedName(Alias);

            return text;
        }
    }

    public class OrmLiteDataParameter : IDbDataParameter
    {
        public DbType DbType { get; set; }
        public ParameterDirection Direction { get; set; }
        public bool IsNullable { get; set; }
        public string ParameterName { get; set; }
        public string SourceColumn { get; set; }
        public DataRowVersion SourceVersion { get; set; }
        public object Value { get; set; }
        public byte Precision { get; set; }
        public byte Scale { get; set; }
        public int Size { get; set; }
    }

    public static class DbDataParameterExtensions
    {
        public static IDbDataParameter CreateParam(this IDbConnection db,
            string name,
            object value=null,
            Type fieldType = null,
            DbType? dbType=null,
            byte? precision=null,
            byte? scale=null,
            int? size=null)
        {
            return db.GetDialectProvider().CreateParam(name, value, fieldType, dbType, precision, scale, size);
        }

        public static IDbDataParameter CreateParam(this IOrmLiteDialectProvider dialectProvider,
            string name,
            object value = null,
            Type fieldType = null,
            DbType? dbType = null,
            byte? precision = null,
            byte? scale = null,
            int? size = null)
        {
            var p = dialectProvider.CreateParam();

            p.ParameterName = dialectProvider.GetParam(name);
            
            dialectProvider.ConfigureParam(p, value, dbType);

            if (precision != null)
                p.Precision = precision.Value;
            if (scale != null)
                p.Scale = scale.Value;
            if (size != null)
                p.Size = size.Value;

            return p;
        }

        internal static void ConfigureParam(this IOrmLiteDialectProvider dialectProvider, IDbDataParameter p, object value, DbType? dbType)
        {
            if (value != null)
            {
                dialectProvider.InitDbParam(p, value.GetType());
                p.Value = dialectProvider.GetParamValue(value, value.GetType());
            }
            else
            {
                p.Value = DBNull.Value;
            }

            // Can't check DbType in PostgreSQL before p.Value is assinged 
            if (p.Value is string strValue && strValue.Length > p.Size)
            {
                var stringConverter = dialectProvider.GetStringConverter();
                p.Size = strValue.Length > stringConverter.StringLength
                    ? strValue.Length
                    : stringConverter.StringLength;
            }

            if (dbType != null)
                p.DbType = dbType.Value;
        }

        public static IDbDataParameter AddQueryParam(this IOrmLiteDialectProvider dialectProvider,
            IDbCommand dbCmd,
            object value,
            FieldDefinition fieldDef) => dialectProvider.AddParam(dbCmd, value, fieldDef, paramFilter: dialectProvider.InitQueryParam);

        public static IDbDataParameter AddUpdateParam(this IOrmLiteDialectProvider dialectProvider,
            IDbCommand dbCmd,
            object value,
            FieldDefinition fieldDef) => dialectProvider.AddParam(dbCmd, value, fieldDef, paramFilter: dialectProvider.InitUpdateParam);

        public static IDbDataParameter AddParam(this IOrmLiteDialectProvider dialectProvider,
            IDbCommand dbCmd,
            object value,
            FieldDefinition fieldDef, Action<IDbDataParameter> paramFilter)
        {
            var paramName = dbCmd.Parameters.Count.ToString();
            var parameter = dialectProvider.CreateParam(paramName, value, fieldDef?.ColumnType);
            
            paramFilter?.Invoke(parameter);

            if (fieldDef != null)
                dialectProvider.SetParameter(fieldDef, parameter);

            dbCmd.Parameters.Add(parameter);
            return parameter;
        }

        public static string GetInsertParam(this IOrmLiteDialectProvider dialectProvider,
            IDbCommand dbCmd,
            object value,
            FieldDefinition fieldDef)
        {
            var p = dialectProvider.AddUpdateParam(dbCmd, value, fieldDef);
            return fieldDef.CustomInsert != null
                ? string.Format(fieldDef.CustomInsert, p.ParameterName)
                : p.ParameterName;
        }

        public static string GetUpdateParam(this IOrmLiteDialectProvider dialectProvider,
            IDbCommand dbCmd,
            object value,
            FieldDefinition fieldDef)
        {
            var p = dialectProvider.AddUpdateParam(dbCmd, value, fieldDef);
            return fieldDef.CustomUpdate != null
                ? string.Format(fieldDef.CustomUpdate, p.ParameterName)
                : p.ParameterName;
        }
    }
}

