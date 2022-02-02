using System;
using System.Data;

namespace ServiceStack.OrmLite.Converters
{
    public class GuidConverter : OrmLiteConverter
    {
        public override string ColumnDefinition => "GUID";
        public override DbType DbType => DbType.Guid;
    }
}