using System;
using System.Data;

namespace ServiceStack.OrmLite.Converters
{
    public abstract class IntegerConverter : NativeValueOrmLiteConverter
    {
        public override string ColumnDefinition => "INTEGER";
        public override DbType DbType => DbType.Int32;

        public override object ToDbValue(Type fieldType, object value)
        {
            return this.ConvertNumber(fieldType, value);
        }

        public override object FromDbValue(Type fieldType, object value)
        {
            return this.ConvertNumber(fieldType, value);
        }
    }

    public class ByteConverter : IntegerConverter
    {
        public override DbType DbType => DbType.Byte;
    }

    public class SByteConverter : IntegerConverter
    {
        public override DbType DbType => DbType.SByte;
    }

    public class Int16Converter : IntegerConverter
    {
        public override DbType DbType => DbType.Int16;
    }

    public class UInt16Converter : IntegerConverter
    {
        public override DbType DbType => DbType.UInt16;
    }

    public class Int32Converter : IntegerConverter {}

    public class UInt32Converter : IntegerConverter
    {
        public override DbType DbType => DbType.UInt32;
    }

    public class Int64Converter : IntegerConverter
    {
        public override DbType DbType => DbType.Int64;
        public override string ColumnDefinition => "BIGINT";
    }

    public class UInt64Converter : IntegerConverter
    {
        public override DbType DbType => DbType.UInt64;
        public override string ColumnDefinition => "BIGINT";
    }

}