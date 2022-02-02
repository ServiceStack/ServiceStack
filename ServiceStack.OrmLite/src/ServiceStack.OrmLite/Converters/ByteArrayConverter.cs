using System.Data;

namespace ServiceStack.OrmLite.Converters
{
    public class ByteArrayConverter : OrmLiteConverter
    {
        public override string ColumnDefinition => "BLOB";
        public override DbType DbType => DbType.Binary;
    }
}