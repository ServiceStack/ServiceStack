//
// https://github.com/ServiceStack/ServiceStack.Text
// ServiceStack.Text: .NET C# POCO JSON, JSV and CSV Text Serializers.
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2012 ServiceStack, Inc. All Rights Reserved.
//
// Licensed under the same terms of ServiceStack.
//

using System;
using ServiceStack.Text.Json;

namespace ServiceStack.Text.Common;

public static class DeserializeBuiltin<T>
{
    private static readonly ParseStringSpanDelegate CachedParseFn;
    static DeserializeBuiltin()
    {
        CachedParseFn = GetParseStringSpanFn();
    }

    public static ParseStringDelegate Parse => v => CachedParseFn(v.AsSpan());

    public static ParseStringSpanDelegate ParseStringSpan => CachedParseFn;

    private static ParseStringDelegate GetParseFn() => v => GetParseStringSpanFn()(v.AsSpan());

    private static ParseStringSpanDelegate GetParseStringSpanFn()
    {
        var nullableType = Nullable.GetUnderlyingType(typeof(T));
        if (nullableType == null)
        {
            var typeCode = typeof(T).GetTypeCode();
            switch (typeCode)
            {
                case TypeCode.Boolean:
                    return value => value.ParseBoolean();
                case TypeCode.SByte:
                    return SignedInteger<sbyte>.ParseObject;
                case TypeCode.Byte:
                    return UnsignedInteger<byte>.ParseObject;
                case TypeCode.Int16:
                    return SignedInteger<short>.ParseObject;
                case TypeCode.UInt16:
                    return UnsignedInteger<ushort>.ParseObject;
                case TypeCode.Int32:
                    return SignedInteger<int>.ParseObject;
                case TypeCode.UInt32:
                    return UnsignedInteger<uint>.ParseObject;
                case TypeCode.Int64:
                    return SignedInteger<long>.ParseObject;
                case TypeCode.UInt64:
                    return UnsignedInteger<ulong>.ParseObject;

                case TypeCode.Single:
                    return value => MemoryProvider.Instance.ParseFloat(value);
                case TypeCode.Double:
                    return value => MemoryProvider.Instance.ParseDouble(value);
                case TypeCode.Decimal:
                    return value => MemoryProvider.Instance.ParseDecimal(value);
                case TypeCode.DateTime:
                    return value => DateTimeSerializer.ParseShortestXsdDateTime(value.ToString());
                case TypeCode.Char:
                    return value => value.Length == 0 ? (char)0 : value.Length == 1 ? value[0] : JsonTypeSerializer.Unescape(value)[0];
            }

            if (typeof(T) == typeof(Guid))
                return value => value.ParseGuid();
            if (typeof(T) == typeof(DateTimeOffset))
                return value => DateTimeSerializer.ParseDateTimeOffset(value.ToString());
            if (typeof(T) == typeof(TimeSpan))
                return value => DateTimeSerializer.ParseTimeSpan(value.ToString());
#if NET6_0_OR_GREATER 
                if (typeof(T) == typeof(DateOnly))
                    return value => DateOnly.FromDateTime(DateTimeSerializer.ParseShortestXsdDateTime(value.ToString()));
                if (typeof(T) == typeof(TimeOnly))
                    return value => TimeOnly.FromTimeSpan(DateTimeSerializer.ParseTimeSpan(value.ToString()));
#endif
        }
        else
        {
            var typeCode = nullableType.GetTypeCode();
            switch (typeCode)
            {
                case TypeCode.Boolean:
                    return value => value.IsNullOrEmpty() 
                        ? (bool?)null 
                        : value.ParseBoolean();
                case TypeCode.SByte:
                    return SignedInteger<sbyte>.ParseNullableObject;
                case TypeCode.Byte:
                    return UnsignedInteger<byte>.ParseNullableObject;
                case TypeCode.Int16:
                    return SignedInteger<short>.ParseNullableObject;
                case TypeCode.UInt16:
                    return UnsignedInteger<ushort>.ParseNullableObject;
                case TypeCode.Int32:
                    return SignedInteger<int>.ParseNullableObject;
                case TypeCode.UInt32:
                    return UnsignedInteger<uint>.ParseNullableObject;
                case TypeCode.Int64:
                    return SignedInteger<long>.ParseNullableObject;
                case TypeCode.UInt64:
                    return UnsignedInteger<ulong>.ParseNullableObject;

                case TypeCode.Single:
                    return value => value.IsNullOrEmpty() ? (float?)null : value.ParseFloat();
                case TypeCode.Double:
                    return value => value.IsNullOrEmpty() ? (double?)null : value.ParseDouble();
                case TypeCode.Decimal:
                    return value => value.IsNullOrEmpty() ? (decimal?)null : value.ParseDecimal();
                case TypeCode.DateTime:
                    return value => DateTimeSerializer.ParseShortestNullableXsdDateTime(value.ToString());
                case TypeCode.Char:
                    return value => value.IsEmpty ? (char?)null : value.Length == 1 ? value[0] : JsonTypeSerializer.Unescape(value)[0];
            }

            if (typeof(T) == typeof(TimeSpan?))
                return value => DateTimeSerializer.ParseNullableTimeSpan(value.ToString());
            if (typeof(T) == typeof(Guid?))
                return value => value.IsNullOrEmpty() ? (Guid?)null : value.ParseGuid();
            if (typeof(T) == typeof(DateTimeOffset?))
                return value => DateTimeSerializer.ParseNullableDateTimeOffset(value.ToString());
#if NET6_0_OR_GREATER
                if (typeof(T) == typeof(DateOnly?))
                    return value => value.IsNullOrEmpty() ? (DateOnly?)null : DateOnly.FromDateTime(DateTimeSerializer.ParseShortestXsdDateTime(value.ToString()));
                if (typeof(T) == typeof(TimeOnly?))
                    return value => value.IsNullOrEmpty() ? (TimeOnly?)null : TimeOnly.FromTimeSpan(DateTimeSerializer.ParseTimeSpan(value.ToString()));
#endif
        }

        return null;
    }
}