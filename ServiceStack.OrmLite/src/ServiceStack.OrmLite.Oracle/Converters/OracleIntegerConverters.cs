using System.Data;
using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.Oracle.Converters
{
    public class OracleInt64Converter : Int64Converter
    {
        public override string ColumnDefinition
        {
            get { return "NUMERIC(18)"; }
        }
    }

    public class OracleSByteConverter : SByteConverter
    {
        public override DbType DbType
        {
            get { return DbType.Byte; }
        }
    }

    public class OracleUInt16Converter : UInt16Converter
    {
        public override DbType DbType
        {
            get { return DbType.Decimal; }
        }
    }

    public class OracleUInt32Converter : UInt32Converter
    {
        public override DbType DbType
        {
            get { return DbType.Decimal; }
        }

        public override string ColumnDefinition
        {
            get { return "NUMERIC(18)"; }
        }
    }

    public class OracleUInt64Converter : UInt64Converter
    {
        public override DbType DbType
        {
            get { return DbType.Decimal; }
        }

        public override string ColumnDefinition
        {
            get { return "NUMERIC(18)"; }
        }
    }
}