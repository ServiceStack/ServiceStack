using Kdbndp;
using ServiceStack.OrmLite.Converters;
using System;
using System.Collections.Generic;
using System.Data;
using KdbndpTypes;

namespace ServiceStack.OrmLite.Kingbase.Converters
{
    public class KingbaseSqlHstoreConverter : ReferenceTypeConverter
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
            var sqlParam = (KdbndpParameter)p;
            sqlParam.KdbndpDbType = KdbndpDbType.Hstore;
            base.InitDbParam(p, fieldType);
        }

        public override string GetColumnDefinition(int? stringLength) => ColumnDefinition;
    }
}