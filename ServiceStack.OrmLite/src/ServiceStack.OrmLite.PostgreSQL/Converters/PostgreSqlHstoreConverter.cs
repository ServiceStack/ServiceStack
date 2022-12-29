using System;
using System.Collections.Generic;
using System.Data;
using Npgsql;
using NpgsqlTypes;
using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.PostgreSQL.Converters
{
    public class PostgreSqlHstoreConverter : ReferenceTypeConverter
    {
        public override string ColumnDefinition => "hstore";

        public override DbType DbType => DbType.Object;

        public override object FromDbValue(Type fieldType, object value)
        {
            return (IDictionary<string, string>)value;
        }

        public override object ToDbValue(Type fieldType, object value)
        {
            return (IDictionary<string, string>)value;
        }

        public override void InitDbParam(IDbDataParameter p, Type fieldType)
        {
            var sqlParam = (NpgsqlParameter)p;
            sqlParam.NpgsqlDbType = NpgsqlDbType.Hstore;
            base.InitDbParam(p, fieldType);
        }

        public override string GetColumnDefinition(int? stringLength) => ColumnDefinition;
    }
}