using System;
using System.Data;
using System.Linq;
using ServiceStack.OrmLite.Converters;

namespace ServiceStack.OrmLite.Sqlite.Converters;

public class SqliteGuidConverter : GuidConverter
{
    public override string ColumnDefinition => "CHAR(36)";

    public override string ToQuotedString(Type fieldType, object value)
    {
        var guid = (Guid)value;
        var bytes = guid.ToByteArray();
        var fmt = "x'" + BitConverter.ToString(bytes).Replace("-", "") + "'";
        return fmt;
    }

    public override object GetValue(IDataReader reader, int columnIndex, object[] values)
    {
        if (values != null)
        {
            if (values[columnIndex] == DBNull.Value)
                return null;
        }
        else
        {
            if (reader.IsDBNull(columnIndex))
                return null;
        }

        return reader.GetGuid(columnIndex);
    }
}
    
public class SqliteDataGuidConverter : GuidConverter
{
    public override string ColumnDefinition => "CHAR(36)";

    public override string ToQuotedString(Type fieldType, object value)
    {
        var guid = (Guid)value;
        var bytes = guid.ToByteArray();
        SwapEndian(bytes);
        var p = BitConverter.ToString(bytes).Replace("-","");
        var fmt = "'" + p.Substring(0,8) + "-" 
                  + p.Substring(8,4) + "-" 
                  + p.Substring(12,4) + "-" 
                  + p.Substring(16,4) + "-"
                  + p.Substring(20) + "'";
        return fmt;
    }
        
    public static void SwapEndian(byte[] guid)
    {
        _ = guid ?? throw new ArgumentNullException(nameof(guid));
        Swap(guid, 0, 3);
        Swap(guid, 1, 2);
        Swap(guid, 4, 5);
        Swap(guid, 6, 7);
    }

    private static void Swap(byte[] array, int index1, int index2)
    {
        (array[index1], array[index2]) = (array[index2], array[index1]);
    }        
    public override object GetValue(IDataReader reader, int columnIndex, object[] values)
    {
        if (values != null)
        {
            if (values[columnIndex] == DBNull.Value)
                return null;
        }
        else
        {
            if (reader.IsDBNull(columnIndex))
                return null;
        }

        return reader.GetGuid(columnIndex);
    }
}