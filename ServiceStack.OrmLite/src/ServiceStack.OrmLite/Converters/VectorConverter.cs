using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ServiceStack.OrmLite.Converters;

/// <summary>
/// Converts the float[] of a [Vector] column to and from the RDBMS's vector type. Vectors are sent as the bytes
/// of their 32-bit floats, which MySQL, MariaDB and SQLite's sqlite-vec extension store.
/// </summary>
public class VectorConverter : OrmLiteConverter
{
    public override string ColumnDefinition => "BLOB";
    public override DbType DbType => DbType.Binary;

    public override object ToDbValue(Type fieldType, object value) =>
        value == null ? null : ToBytes(ToFloats(value));

    public override object FromDbValue(Type fieldType, object value) => ToFloats(value);

    public override string ToQuotedString(Type fieldType, object value) =>
        "'" + ToText(ToFloats(value)) + "'";

    /// <summary>
    /// The vector of any value an RDBMS or App represents one with: floats, their bytes, or text like [1,2,3]
    /// </summary>
    public static float[] ToFloats(object value)
    {
        switch (value)
        {
            case null:
            case DBNull:
                return null;
            case float[] floats:
                return floats;
            case byte[] bytes:
                return FromBytes(bytes);
            case string text:
                return FromText(text);
            case ReadOnlyMemory<float> memory:
                return memory.ToArray();
            case Memory<float> memory:
                return memory.ToArray();
            case IEnumerable<float> enumerable:
                return enumerable.ToArray();
            case IEnumerable<double> doubles:
                return doubles.Select(x => (float)x).ToArray();
        }

        // e.g. Microsoft.Data.SqlTypes.SqlVector<float> and Pgvector.Vector
        var type = value.GetType();
        if (type.GetProperty("IsNull")?.GetValue(value) is true)
            return null;
        var memoryValue = type.GetProperty("Memory")?.GetValue(value);
        if (memoryValue is ReadOnlyMemory<float> readOnlyMemory)
            return readOnlyMemory.ToArray();
        if (type.GetMethod("ToArray", Type.EmptyTypes)?.Invoke(value, null) is float[] array)
            return array;

        throw new NotSupportedException($"Cannot convert {type.Name} to a float[] vector");
    }

    public static byte[] ToBytes(float[] vector)
    {
        var bytes = new byte[vector.Length * sizeof(float)];
        Buffer.BlockCopy(vector, 0, bytes, 0, bytes.Length);
        if (!BitConverter.IsLittleEndian)
            ReverseEachFloat(bytes);
        return bytes;
    }

    public static float[] FromBytes(byte[] bytes)
    {
        if (bytes.Length % sizeof(float) != 0)
            throw new FormatException($"A vector's bytes must be a multiple of {sizeof(float)}, but there are {bytes.Length}");

        if (!BitConverter.IsLittleEndian)
        {
            bytes = (byte[])bytes.Clone();
            ReverseEachFloat(bytes);
        }
        var vector = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, vector, 0, bytes.Length);
        return vector;
    }

    private static void ReverseEachFloat(byte[] bytes)
    {
        for (var i = 0; i < bytes.Length; i += sizeof(float))
            Array.Reverse(bytes, i, sizeof(float));
    }

    /// <summary>
    /// The text PostgreSQL's pgvector and SQL Server use for vectors, e.g. [1,2.5,3]
    /// </summary>
    public static string ToText(float[] vector)
    {
        var sb = new StringBuilder(vector.Length * 12).Append('[');
        for (var i = 0; i < vector.Length; i++)
        {
            if (i > 0)
                sb.Append(',');
            // The shortest text that reads back as the same float
            sb.Append(vector[i].ToString("R", CultureInfo.InvariantCulture));
        }
        return sb.Append(']').ToString();
    }

    public static float[] FromText(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.Length < 2 || trimmed[0] != '[' || trimmed[trimmed.Length - 1] != ']')
            throw new FormatException("A vector's text must be in the format [1,2,3]");

        var values = trimmed.Substring(1, trimmed.Length - 2);
        if (values.Trim().Length == 0)
            return [];

        var parts = values.Split(',');
        var vector = new float[parts.Length];
        for (var i = 0; i < parts.Length; i++)
            vector[i] = float.Parse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture);
        return vector;
    }
}

/// <summary>
/// Sends vectors as text like [1,2,3], which PostgreSQL's pgvector and SQL Server convert to their vector type
/// </summary>
public class VectorTextConverter : VectorConverter
{
    public override string ColumnDefinition => "VECTOR";
    public override DbType DbType => DbType.String;

    public override object ToDbValue(Type fieldType, object value) =>
        value == null ? null : ToText(ToFloats(value));
}
