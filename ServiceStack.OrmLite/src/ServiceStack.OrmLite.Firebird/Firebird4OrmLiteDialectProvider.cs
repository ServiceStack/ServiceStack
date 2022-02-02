using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;
using System.Linq;
using FirebirdSql.Data.FirebirdClient;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Firebird.Converters;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Firebird
{
    public class Firebird4OrmLiteDialectProvider : FirebirdOrmLiteDialectProvider
    {
        private readonly bool usesCompactGuid;

        public new static List<string> RESERVED = new List<string>(new[] {
            "ADD","ADMIN","ALL","ALTER","AND","ANY","AS","AT","AVG","BEGIN","BETWEEN","BIGINT","BIT_LENGTH","BLOB","BOTH","BY","CASE","CAST",
            "CHAR","CHAR_LENGTH","CHARACTER","CHARACTER_LENGTH","CHECK","CLOSE","COLLATE","COLUMN","COMMIT","CONNECT","CONSTRAINT","COUNT","CREATE",
            "CROSS","CURRENT","CURRENT_CONNECTION","CURRENT_DATE","CURRENT_ROLE","CURRENT_TIME","CURRENT_TIMESTAMP","CURRENT_TRANSACTION","CURRENT_USER",
            "CURSOR","DATE","DAY","DEC","DECIMAL","DECLARE","DEFAULT","DELETE","DISCONNECT","DISTINCT","DOUBLE","DROP","ELSE","END","ESCAPE",
            "EXECUTE","EXISTS","EXTERNAL","EXTRACT","FETCH","FILTER","FLOAT","FOR","FOREIGN","FROM","FULL","FUNCTION","GDSCODE","GLOBAL","GRANT","GROUP",
            "HAVING","HOUR","IN","INDEX","INNER","INSENSITIVE","INSERT","INT","INTEGER","INTO","IS","JOIN","LEADING","LEFT","LIKE","LONG","LOWER","MAX",
            "MAXIMUM_SEGMENT","MERGE","MIN","MINUTE","MONTH","NATIONAL","NATURAL","NCHAR","NO","NOT","NULL","NUMERIC","OCTET_LENGTH","OF","ON","ONLY",
            "OPEN","OR","ORDER","OUTER","PARAMETER","PLAN","POSITION","POST_EVENT","PRECISION","PRIMARY","PROCEDURE","RDB$DB_KEY","REAL","RECORD_VERSION",
            "RECREATE","RECURSIVE","REFERENCES","RELEASE","RETURNING_VALUES","RETURNS","REVOKE","RIGHT","ROLLBACK","ROW_COUNT","ROWS","SAVEPOINT","SECOND",
            "SELECT","SENSITIVE","SET","SIMILAR","SMALLINT","SOME","SQLCODE","SQLSTATE","START","SUM","TABLE","THEN","TIME","TIMESTAMP","TO","TRAILING",
            "TRIGGER","TRIM","UNION","UNIQUE","UPDATE","UPPER","USER","USING","VALUE","VALUES","VARCHAR","VARIABLE","VARYING","VIEW","WHEN","WHERE","WHILE",
            "WITH","YEAR",
            "PASSWORD","ACTIVE","LEFT","DATETIME","TYPE","KEY",
            // new in FB3
            "INSERTING","UPDATING","DELETING","REGR_AVGX","SCROLL","CORR","REGR_AVGY","COVAR_POP","REGR_COUNT","STDDEV_POP",
            "COVAR_SAMP","REGR_INTERCEPT","STDDEV_SAMP","REGR_R2","TRUE","DETERMINISTIC","REGR_SLOPE","UNKNOWN","FALSE","REGR_SXX",
            "REGR_SXY","VAR_POP","OFFSET","REGR_SYY","VAR_SAMP","OVER","RETURN","RDB$RECORD_VERSION","ROW","BOOLEAN",
            // new in FB4
            "BINARY","VARBINARY","DECFLOAT"
        });

        public new static Firebird4OrmLiteDialectProvider Instance = new Firebird4OrmLiteDialectProvider();

        public Firebird4OrmLiteDialectProvider() : this(true) { }

        public Firebird4OrmLiteDialectProvider(bool compactGuid): base(compactGuid)
        {
            usesCompactGuid = compactGuid;

            // FB4 now has identity columns
            base.AutoIncrementDefinition = " GENERATED ALWAYS AS IDENTITY ";
            NamingStrategy = new Firebird4NamingStrategy();

            base.RemoveConverter<bool>();

            this.Variables = new Dictionary<string, string>
            {
                { OrmLiteVariables.SystemUtc, "CURRENT_TIMESTAMP" },
                { OrmLiteVariables.MaxText, "VARCHAR(1000)" },
                { OrmLiteVariables.MaxTextUnicode, "VARCHAR(2048)" },
                { OrmLiteVariables.True, SqlBool(true) },                
                { OrmLiteVariables.False, SqlBool(false) },                
            };
        }

        public override string ToCreateTableStatement(Type tableType)
        {
            var sbColumns = StringBuilderCache.Allocate();
            var sbConstraints = StringBuilderCacheAlt.Allocate();

            var sbPk = new StringBuilder();

            var modelDef = GetModel(tableType);
            foreach (var fieldDef in CreateTableFieldsStrategy(modelDef))
            {
                if (fieldDef.CustomSelect != null || (fieldDef.IsComputed && !fieldDef.IsPersisted))
                    continue;

                if (fieldDef.IsPrimaryKey)
                    sbPk.AppendFormat(sbPk.Length != 0 ? ",{0}" : "{0}", GetQuotedColumnName(fieldDef.FieldName));

                if (sbColumns.Length != 0)
                    sbColumns.Append(", \n  ");

                var columnDefinition = GetColumnDefinition(fieldDef);
                sbColumns.Append(columnDefinition);

                if (fieldDef.ForeignKey == null || OrmLiteConfig.SkipForeignKeys)
                    continue;

                var refModelDef = GetModel(fieldDef.ForeignKey.ReferenceType);

                var fkName = NamingStrategy.ApplyNameRestrictions(fieldDef.ForeignKey.GetForeignKeyName(modelDef, refModelDef, NamingStrategy, fieldDef)).ToLower();
                sbConstraints.AppendFormat(", \n\n  CONSTRAINT {0} FOREIGN KEY ({1}) REFERENCES {2} ({3})",
                    GetQuotedName(fkName),
                    GetQuotedColumnName(fieldDef.FieldName),
                    GetQuotedTableName(refModelDef),
                    GetQuotedColumnName(refModelDef.PrimaryKey.FieldName));

                sbConstraints.Append(GetForeignKeyOnDeleteClause(fieldDef.ForeignKey));
                sbConstraints.Append(GetForeignKeyOnUpdateClause(fieldDef.ForeignKey));
            }

            if (sbPk.Length != 0)
                sbColumns.AppendFormat(", \n  PRIMARY KEY({0})", sbPk);

            var sql = $"RECREATE TABLE {GetQuotedTableName(modelDef)} \n(\n  {StringBuilderCache.ReturnAndFree(sbColumns)}{StringBuilderCacheAlt.ReturnAndFree(sbConstraints)} \n); \n";

            return sql;
        }

        public override List<string> ToCreateSequenceStatements(Type tableType)
        {
            var gens = new List<string>();
            var modelDef = GetModel(tableType);

            foreach (var fieldDef in modelDef.FieldDefinitions)
            {
                if (!fieldDef.AutoIncrement || fieldDef.Sequence.IsNullOrEmpty()) continue;
                
                // https://firebirdsql.org/refdocs/langrefupd21-ddl-sequence.html
                var sequence = Sequence(modelDef.ModelName, fieldDef.FieldName, fieldDef.Sequence).ToUpper();
                gens.Add(GetCreateSequenceSql(sequence));
            }
            return gens;
        }

        protected override void EnsureAutoIncrementSequence(ModelDefinition modelDef, FieldDefinition fieldDef)
        {
            if (fieldDef.AutoIncrement && !string.IsNullOrEmpty(fieldDef.Sequence))
            {
                fieldDef.Sequence = Sequence(modelDef.ModelName, fieldDef.FieldName, fieldDef.Sequence);
            }
        }

        public override string GetColumnDefinition(FieldDefinition fieldDef)
        {
            var fieldDefinition = ResolveFragment(fieldDef.CustomFieldDefinition) 
                ?? GetColumnTypeDefinition(fieldDef.ColumnType, fieldDef.FieldLength, fieldDef.Scale);

            var sql = StringBuilderCache.Allocate();
            sql.AppendFormat("{0} {1}", GetQuotedColumnName(fieldDef.FieldName), fieldDefinition);

            var defaultValue = GetDefaultValue(fieldDef);
            if (fieldDef.IsRowVersion)
            {
                sql.AppendFormat(DefaultValueFormat, 1L);
            }
            else if (!string.IsNullOrEmpty(defaultValue))
            {
                sql.AppendFormat(DefaultValueFormat, defaultValue);
            }

            if (fieldDef.AutoIncrement && string.IsNullOrEmpty(fieldDef.Sequence))
            {
                sql.Append(AutoIncrementDefinition);
            }
            else
            // Identity columns must accept null to generate a new value.
            if (!fieldDef.IsNullable)
            {
                sql.Append(" NOT NULL");
            }
            if (fieldDef.IsUniqueConstraint)
            {
                sql.Append(" UNIQUE");
            }

            return StringBuilderCacheAlt.ReturnAndFree(sql);
        }

        public override void PrepareParameterizedInsertStatement<T>(IDbCommand cmd, ICollection<string> insertFields = null, 
            Func<FieldDefinition,bool> shouldInclude=null)
        {
            var sbColumnNames = StringBuilderCache.Allocate();
            var sbColumnValues = StringBuilderCacheAlt.Allocate();
            var sbReturningColumns = StringBuilderCacheAlt.Allocate();
            var modelDef = OrmLiteUtils.GetModelDefinition(typeof(T));

            cmd.Parameters.Clear();
            cmd.CommandTimeout = OrmLiteConfig.CommandTimeout;

            var fieldDefs = GetInsertFieldDefinitions(modelDef, insertFields);
            foreach (var fieldDef in fieldDefs)
            {
                if (ShouldReturnOnInsert(modelDef, fieldDef))
                {
                    if (sbReturningColumns.Length > 0)
                        sbReturningColumns.Append(",");
                    sbReturningColumns.Append(GetQuotedColumnName(fieldDef.FieldName));
                }

                if ((ShouldSkipInsert(fieldDef) || (fieldDef.AutoIncrement && string.IsNullOrEmpty(fieldDef.Sequence)))
                    && shouldInclude?.Invoke(fieldDef) != true)
                    continue;

                if (sbColumnNames.Length > 0)
                    sbColumnNames.Append(",");
                if (sbColumnValues.Length > 0)
                    sbColumnValues.Append(",");

                try
                {
                    sbColumnNames.Append(GetQuotedColumnName(fieldDef.FieldName));

                    // in FB4 only use 'next value for' if the fielddef has a sequence explicitly.
                    if (fieldDef.AutoIncrement && !string.IsNullOrEmpty(fieldDef.Sequence))
                    {
                        EnsureAutoIncrementSequence(modelDef, fieldDef);
                        sbColumnValues.Append("NEXT VALUE FOR " + fieldDef.Sequence);
                    }
                    if (fieldDef.AutoId && usesCompactGuid)
                    {
                        sbColumnValues.Append("GEN_UUID()");
                    }
                    else
                    {
                        sbColumnValues.Append(this.GetParam(SanitizeFieldNameForParamName(fieldDef.FieldName),fieldDef.CustomInsert));
                        AddParameter(cmd, fieldDef);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("ERROR in PrepareParameterizedInsertStatement(): " + ex.Message, ex);
                    throw;
                }
            }

            var strReturning = StringBuilderCacheAlt.ReturnAndFree(sbReturningColumns);
            cmd.CommandText = string.Format("INSERT INTO {0} ({1}) VALUES ({2}) {3}",
                GetQuotedTableName(modelDef), 
                StringBuilderCache.ReturnAndFree(sbColumnNames), 
                StringBuilderCacheAlt.ReturnAndFree(sbColumnValues),
                strReturning.Length > 0 ? "RETURNING " + strReturning : "");
        }
    }
}

