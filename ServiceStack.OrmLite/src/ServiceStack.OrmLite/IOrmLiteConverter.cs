using System;
using System.Data;
using ServiceStack.Logging;

namespace ServiceStack.OrmLite;

public interface IOrmLiteConverter
{
    IOrmLiteDialectProvider DialectProvider { get; set; }
        
    DbType DbType { get; }

    string ColumnDefinition { get; }

    string ToQuotedString(Type fieldType, object value);

    void InitDbParam(IDbDataParameter p, Type fieldType);

    object ToDbValue(Type fieldType, object value);

    object FromDbValue(Type fieldType, object value);

    object GetValue(IDataReader reader, int columnIndex, object[] values);
}

public interface IHasColumnDefinitionLength
{
    string GetColumnDefinition(int? length);
}

public interface IHasColumnDefinitionPrecision
{
    string GetColumnDefinition(int? precision, int? scale);
}

public abstract class OrmLiteConverter : IOrmLiteConverter
{
    public static ILog Log => OrmLiteLog.Log;

    /// <summary>
    /// RDBMS Dialect this Converter is for. Injected at registration.
    /// </summary>
    public IOrmLiteDialectProvider DialectProvider { get; set; }

    /// <summary>
    /// SQL Column Definition used in CREATE Table. 
    /// </summary>
    public abstract string ColumnDefinition { get; }

    /// <summary>
    /// Used in DB Params. Defaults to DbType.String
    /// </summary>
    public virtual DbType DbType => DbType.String;

    /// <summary>
    /// Quoted Value in SQL Statement
    /// </summary>
    public virtual string ToQuotedString(Type fieldType, object value)
    {
        return DialectProvider.GetQuotedValue(value.ToString());
    }

    /// <summary>
    /// Customize how DB Param is initialized. Useful for supporting RDBMS-specific Types.
    /// </summary>
    public virtual void InitDbParam(IDbDataParameter p, Type fieldType)
    {
        p.DbType = DbType;
    }

    /// <summary>
    /// Parameterized value in parameterized queries
    /// </summary>
    public virtual object ToDbValue(Type fieldType, object value)
    {
        return value;
    }

    /// <summary>
    /// Value from DB to Populate on POCO Data Model with
    /// </summary>
    public virtual object FromDbValue(Type fieldType, object value)
    {
        return value;
    }

    /// <summary>
    /// Retrieve Value from ADO.NET IDataReader. Defaults to reader.GetValue()
    /// </summary>
    public virtual object GetValue(IDataReader reader, int columnIndex, object[] values)
    {
        var value = values != null 
            ? values[columnIndex]
            : reader.GetValue(columnIndex);

        return value == DBNull.Value ? null : value;
    }
}

/// <summary>
/// For Types that are natively supported by RDBMS's and shouldn't be quoted
/// </summary>
public abstract class NativeValueOrmLiteConverter : OrmLiteConverter
{
    public override string ToQuotedString(Type fieldType, object value)
    {
        return value.ToString();
    }
}

public static class OrmLiteConverterExtensions
{
    public static object ConvertNumber(this IOrmLiteConverter converter, Type toIntegerType, object value)
    {
        return converter.DialectProvider.ConvertNumber(toIntegerType, value);
    }

    public static object ConvertNumber(this IOrmLiteDialectProvider dialectProvider, Type toIntegerType, object value)
    {
        if (value == null)
            return null;
        if (value.GetType() == toIntegerType)
            return value;

        var typeCode = toIntegerType.GetUnderlyingTypeCode();

        // BigInteger does NOT implement IConvertible, so every Convert.ToX below throws
        // InvalidCastException ("Unable to cast object of type 'System.Numerics.BigInteger' to type
        // 'System.IConvertible'") instead of converting it.
        //
        // Providers do hand it back. Firebird 4+ has INT128 and widens SUM() of a BIGINT to it, which the
        // stock FirebirdClient correctly surfaces as BigInteger. Measured on Firebird 5.0.4:
        //     SUM(INTEGER)            -> Int64        SUM(BIGINT)             -> BigInteger
        //     SUM(NUMERIC(18,0))      -> Decimal      SUM(OCTET_LENGTH(blob)) -> BigInteger
        //     COUNT(*)                -> Int64
        // So counting works and summing a BIGINT throws, at runtime, in whichever query hits it first -
        // which is what makes it easy to miss. (OCTET_LENGTH of a BLOB is BIGINT, so summing blob sizes
        // reproduces it without anything in the schema being declared INT128.)
        //
        // The casts below are checked, so a value genuinely too large for the target type still throws
        // OverflowException - which is the correct answer, and not the same as silently truncating it.
        if (value is System.Numerics.BigInteger bigInteger)
        {
            switch (typeCode)
            {
                case TypeCode.Byte:
                    return (byte)bigInteger;
                case TypeCode.SByte:
                    return (sbyte)bigInteger;
                case TypeCode.Int16:
                    return (short)bigInteger;
                case TypeCode.UInt16:
                    return (ushort)bigInteger;
                case TypeCode.Int32:
                    return (int)bigInteger;
                case TypeCode.UInt32:
                    return (uint)bigInteger;
                case TypeCode.Int64:
                    return (long)bigInteger;
                case TypeCode.UInt64:
                    return (ulong)bigInteger;
                case TypeCode.Single:
                    return (float)bigInteger;
                case TypeCode.Double:
                    return (double)bigInteger;
                case TypeCode.Decimal:
                    return (decimal)bigInteger;
            }
        }

        switch (typeCode)
        {
            case TypeCode.Byte:
                return Convert.ToByte(value);
            case TypeCode.SByte:
                return Convert.ToSByte(value);
            case TypeCode.Int16:
                return Convert.ToInt16(value);
            case TypeCode.UInt16:
                return Convert.ToUInt16(value);
            case TypeCode.Int32:
                return Convert.ToInt32(value);
            case TypeCode.UInt32:
                return Convert.ToUInt32(value);
            case TypeCode.Int64:
                return Convert.ToInt64(value);
            case TypeCode.UInt64:
                if (value is byte[] byteValue)
                    return OrmLiteUtils.ConvertToULong(byteValue);
                return Convert.ToUInt64(value);
            case TypeCode.Single:
                return Convert.ToSingle(value);
            case TypeCode.Double:
                return Convert.ToDouble(value);
            case TypeCode.Decimal:
                return Convert.ToDecimal(value);
        }

        var convertedValue = dialectProvider.StringSerializer.DeserializeFromString(value.ToString(), toIntegerType);
        return convertedValue;
    }
}