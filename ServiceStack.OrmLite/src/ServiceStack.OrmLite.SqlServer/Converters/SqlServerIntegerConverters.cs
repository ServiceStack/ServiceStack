using System.Data;
using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.SqlServer.Converters
{
    //throws unknown type exceptions in parameterized queries, e.g: p.DbType = DbType.SByte
    public class SqlServerSByteConverter : SByteConverter
    {
        public override DbType DbType
        {
            get { return DbType.Byte; }
        }
    }

    public class SqlServerUInt16Converter : UInt16Converter
    {
        public override DbType DbType
        {
            get { return DbType.Int16; }
        }
    }

    public class SqlServerUInt32Converter : UInt32Converter
    {
        public override DbType DbType
        {
            get { return DbType.Int32; }
        }
    }

    public class SqlServerUInt64Converter : UInt64Converter
    {
        public override DbType DbType
        {
            get { return DbType.Int64; }
        }
    }
}