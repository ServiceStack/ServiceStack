using System;
using System.Data;
using KdbndpTypes;
using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.Kingbase.Converters.MySql;

public abstract class KingbaseMySqlIntegerConverter : IntegerConverter, IKingbaseConverter
{
    public override string ColumnDefinition => "INT";
    public KdbndpDbType KdbndpDbType => KdbndpDbType.Integer;
}

public class KingbaseMySqlByteConverter : KingbaseMySqlIntegerConverter
{
    public override DbType DbType => DbType.Byte;
}

public class KingbaseMySqlSByteConverter : KingbaseMySqlIntegerConverter
{
    public override DbType DbType => DbType.SByte;
}

public class KingbaseMySqlInt16Converter : KingbaseMySqlIntegerConverter
{
    public override DbType DbType => DbType.Int16;
}

public class KingbaseMySqlUInt16Converter : KingbaseMySqlIntegerConverter
{
    public override DbType DbType =>
        DbType.UInt32; //WARN The parameter type DbType.UInt16 isn't supported by KingbaseES or Kdbndp

    public override object ToDbValue(Type fieldType, object value)
    {
        var result = this.ConvertNumber(typeof(UInt32), value);
        return result;
    }

    public override object FromDbValue(Type fieldType, object value)
    {
        var result = this.ConvertNumber(typeof(UInt32), value);
        return result;
    }
}

public class KingbaseMySqlInt32Converter : KingbaseMySqlIntegerConverter
{
}

public class KingbaseMySqlUInt32Converter : KingbaseMySqlIntegerConverter
{
    public override DbType DbType => DbType.UInt32;
}