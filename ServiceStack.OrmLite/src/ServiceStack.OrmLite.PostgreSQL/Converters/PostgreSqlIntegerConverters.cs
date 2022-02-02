using System;
using System.Data;
using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.PostgreSQL.Converters
{
    //throws unknown type exceptions in parameterized queries, e.g: p.DbType = DbType.SByte
    public class PostrgreSqlSByteConverter : SByteConverter
    {
        public override DbType DbType => DbType.Byte;
    }

    public class PostrgreSqlUInt16Converter : UInt16Converter
    {
        public override DbType DbType => DbType.Int16;

        public override object ToDbValue(Type fieldType, object value)
        {
            return this.ConvertNumber(typeof(short), value);
        }
    }

    public class PostrgreSqlUInt32Converter : UInt32Converter
    {
        public override DbType DbType => DbType.Int32;

        public override object ToDbValue(Type fieldType, object value)
        {
            return this.ConvertNumber(typeof(int), value);
        }
    }

    public class PostrgreSqlUInt64Converter : UInt64Converter
    {
        public override DbType DbType => DbType.Int64;

        public override object ToDbValue(Type fieldType, object value)
        {
            return this.ConvertNumber(typeof(long), value);
        }
    }
}