using System;
using System.Data;
using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.Firebird.Converters
{
    public class FirebirdDateTimeOffsetConverter : DateTimeOffsetConverter
    {
        public override string ColumnDefinition
        {
            get { return "VARCHAR(255)"; }
        }

        public override DbType DbType
        {
            get { return DbType.DateTime; }
        }
    }
}