using System;
using System.Data;

namespace ServiceStack.OrmLite.Sqlite.Converters;

public class SqliteCharConverter : OrmLiteConverter
{
    public override string ColumnDefinition => "CHAR(1)";

    public override object ToDbValue(Type fieldType, object value)
    {
        return value.ToString();
    }

    public override object FromDbValue(Type fieldType, object value)
    {
        return ((string)value)[0];
    }

}