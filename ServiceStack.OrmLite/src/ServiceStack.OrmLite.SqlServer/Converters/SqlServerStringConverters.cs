using System;
using System.Data;
#if MSDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.SqlServer.Converters
{
    public class SqlServerStringConverter : StringConverter
    {
        public override string MaxColumnDefinition => UseUnicode ? "NVARCHAR(MAX)" : "VARCHAR(MAX)";
        
        public override int MaxVarCharLength => UseUnicode ? 4000 : 8000;

        public override string GetColumnDefinition(int? stringLength)
        {
            if (stringLength.GetValueOrDefault() == StringLengthAttribute.MaxText)
                return MaxColumnDefinition;

            var safeLength = Math.Min(
                stringLength.GetValueOrDefault(StringLength), 
                UseUnicode ? 4000 : 8000);

            return UseUnicode
                ? $"NVARCHAR({safeLength})"
                : $"VARCHAR({safeLength})";
        }

        public override void InitDbParam(IDbDataParameter p, Type fieldType)
        {
            base.InitDbParam(p, fieldType);

            if (!(p is SqlParameter sqlParam)) return;

            if (!UseUnicode)
            {
                sqlParam.SqlDbType = SqlDbType.VarChar;
            }
        }
    }
}