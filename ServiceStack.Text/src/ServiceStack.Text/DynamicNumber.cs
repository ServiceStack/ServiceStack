using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using ServiceStack.Text;
using ServiceStack.Text.Common;

namespace ServiceStack
{
    public interface IDynamicNumber
    {
        Type Type { get; }
        object ConvertFrom(object value);
        bool TryParse(string str, out object result);
        string ToString(object value);
        object DefaultValue { get; }

        object add(object lhs, object rhs);
        object sub(object lhs, object rhs);
        object mul(object lhs, object rhs);
        object div(object lhs, object rhs);
        object mod(object lhs, object rhs);
        object pow(object lhs, object rhs);
        object log(object lhs, object rhs);
        object min(object lhs, object rhs);
        object max(object lhs, object rhs);
        int compareTo(object lhs, object rhs);
        
        object bitwiseAnd(object lhs, object rhs);
        object bitwiseOr(object lhs, object rhs);
        object bitwiseXOr(object lhs, object rhs);
        object bitwiseLeftShift(object lhs, object rhs);
        object bitwiseRightShift(object lhs, object rhs);
        object bitwiseNot(object target);
    }

    public class DynamicSByte : IDynamicNumber
    {
        public static DynamicSByte Instance = new DynamicSByte();
        public Type Type => typeof(sbyte);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sbyte Convert(object value) => System.Convert.ToSByte(this.ParseString(value) ?? value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object ConvertFrom(object value) => this.ParseString(value)
            ?? System.Convert.ToSByte(value);

        public bool TryParse(string str, out object result)
        {
            if (sbyte.TryParse(str, out sbyte value))
            {
                result = value;
                return true;
            }
            result = null;
            return false;
        }

        public string ToString(object value) => Convert(value).ToString();
        public object DefaultValue => default(sbyte);

        public object add(object lhs, object rhs) => Convert(lhs) + Convert(rhs);
        public object sub(object lhs, object rhs) => Convert(lhs) - Convert(rhs);
        public object mul(object lhs, object rhs) => Convert(lhs) * Convert(rhs);
        public object div(object lhs, object rhs) => Convert(lhs) / Convert(rhs);
        public object mod(object lhs, object rhs) => Convert(lhs) % Convert(rhs);
        public object min(object lhs, object rhs) => Math.Min(Convert(lhs), Convert(rhs));
        public object max(object lhs, object rhs) => Math.Max(Convert(lhs), Convert(rhs));
        public object pow(object lhs, object rhs) => Math.Pow(Convert(lhs), Convert(rhs));
        public object log(object lhs, object rhs) => Math.Log(Convert(lhs), Convert(rhs));
        public int compareTo(object lhs, object rhs) => Convert(lhs).CompareTo(Convert(rhs));
        
        public object bitwiseAnd(object lhs, object rhs) => Convert(lhs) & Convert(rhs);
        public object bitwiseOr(object lhs, object rhs) => Convert(lhs) | Convert(rhs);
        public object bitwiseXOr(object lhs, object rhs) => Convert(lhs) ^ Convert(rhs);
        public object bitwiseLeftShift(object lhs, object rhs) => Convert(lhs) << Convert(rhs);
        public object bitwiseRightShift(object lhs, object rhs) => Convert(lhs) >> Convert(rhs);
        public object bitwiseNot(object target) => ~Convert(target);
    }

    public class DynamicByte : IDynamicNumber
    {
        public static DynamicByte Instance = new DynamicByte();
        public Type Type => typeof(byte);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte Convert(object value) => System.Convert.ToByte(this.ParseString(value) ?? value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object ConvertFrom(object value) => this.ParseString(value)
            ?? System.Convert.ToByte(value);

        public bool TryParse(string str, out object result)
        {
            if (byte.TryParse(str, out byte value))
            {
                result = value;
                return true;
            }
            result = null;
            return false;
        }

        public string ToString(object value) => Convert(value).ToString();
        public object DefaultValue => default(byte);

        public object add(object lhs, object rhs) => Convert(lhs) + Convert(rhs);
        public object sub(object lhs, object rhs) => Convert(lhs) - Convert(rhs);
        public object mul(object lhs, object rhs) => Convert(lhs) * Convert(rhs);
        public object div(object lhs, object rhs) => Convert(lhs) / Convert(rhs);
        public object mod(object lhs, object rhs) => Convert(lhs) % Convert(rhs);
        public object min(object lhs, object rhs) => Math.Min(Convert(lhs), Convert(rhs));
        public object max(object lhs, object rhs) => Math.Max(Convert(lhs), Convert(rhs));
        public object pow(object lhs, object rhs) => Math.Pow(Convert(lhs), Convert(rhs));
        public object log(object lhs, object rhs) => Math.Log(Convert(lhs), Convert(rhs));
        public int compareTo(object lhs, object rhs) => Convert(lhs).CompareTo(Convert(rhs));

        public object bitwiseAnd(object lhs, object rhs) => Convert(lhs) & Convert(rhs);
        public object bitwiseOr(object lhs, object rhs) => Convert(lhs) | Convert(rhs);
        public object bitwiseXOr(object lhs, object rhs) => Convert(lhs) ^ Convert(rhs);
        public object bitwiseLeftShift(object lhs, object rhs) => Convert(lhs) << Convert(rhs);
        public object bitwiseRightShift(object lhs, object rhs) => Convert(lhs) >> Convert(rhs);
        public object bitwiseNot(object target) => ~Convert(target);
    }

    public class DynamicShort : IDynamicNumber
    {
        public static DynamicShort Instance = new DynamicShort();
        public Type Type => typeof(short);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short Convert(object value) => System.Convert.ToInt16(this.ParseString(value) ?? value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object ConvertFrom(object value) => this.ParseString(value)
            ?? System.Convert.ToInt16(value);

        public bool TryParse(string str, out object result)
        {
            if (short.TryParse(str, out short value))
            {
                result = value;
                return true;
            }
            result = null;
            return false;
        }

        public string ToString(object value) => Convert(value).ToString();
        public object DefaultValue => default(short);

        public object add(object lhs, object rhs) => Convert(lhs) + Convert(rhs);
        public object sub(object lhs, object rhs) => Convert(lhs) - Convert(rhs);
        public object mul(object lhs, object rhs) => Convert(lhs) * Convert(rhs);
        public object div(object lhs, object rhs) => Convert(lhs) / Convert(rhs);
        public object mod(object lhs, object rhs) => Convert(lhs) % Convert(rhs);
        public object min(object lhs, object rhs) => Math.Min(Convert(lhs), Convert(rhs));
        public object max(object lhs, object rhs) => Math.Max(Convert(lhs), Convert(rhs));
        public object pow(object lhs, object rhs) => Math.Pow(Convert(lhs), Convert(rhs));
        public object log(object lhs, object rhs) => Math.Log(Convert(lhs), Convert(rhs));
        public int compareTo(object lhs, object rhs) => Convert(lhs).CompareTo(Convert(rhs));

        public object bitwiseAnd(object lhs, object rhs) => Convert(lhs) & Convert(rhs);
        public object bitwiseOr(object lhs, object rhs) => Convert(lhs) | Convert(rhs);
        public object bitwiseXOr(object lhs, object rhs) => Convert(lhs) ^ Convert(rhs);
        public object bitwiseLeftShift(object lhs, object rhs) => Convert(lhs) << Convert(rhs);
        public object bitwiseRightShift(object lhs, object rhs) => Convert(lhs) >> Convert(rhs);
        public object bitwiseNot(object target) => ~Convert(target);
    }

    public class DynamicUShort : IDynamicNumber
    {
        public static DynamicUShort Instance = new DynamicUShort();
        public Type Type => typeof(ushort);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort Convert(object value) => System.Convert.ToUInt16(this.ParseString(value) ?? value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object ConvertFrom(object value) => this.ParseString(value)
            ?? System.Convert.ToUInt16(value);

        public bool TryParse(string str, out object result)
        {
            if (ushort.TryParse(str, out ushort value))
            {
                result = value;
                return true;
            }
            result = null;
            return false;
        }

        public string ToString(object value) => Convert(value).ToString();
        public object DefaultValue => default(ushort);

        public object add(object lhs, object rhs) => Convert(lhs) + Convert(rhs);
        public object sub(object lhs, object rhs) => Convert(lhs) - Convert(rhs);
        public object mul(object lhs, object rhs) => Convert(lhs) * Convert(rhs);
        public object div(object lhs, object rhs) => Convert(lhs) / Convert(rhs);
        public object mod(object lhs, object rhs) => Convert(lhs) % Convert(rhs);
        public object min(object lhs, object rhs) => Math.Min(Convert(lhs), Convert(rhs));
        public object max(object lhs, object rhs) => Math.Max(Convert(lhs), Convert(rhs));
        public object pow(object lhs, object rhs) => Math.Pow(Convert(lhs), Convert(rhs));
        public object log(object lhs, object rhs) => Math.Log(Convert(lhs), Convert(rhs));
        public int compareTo(object lhs, object rhs) => Convert(lhs).CompareTo(Convert(rhs));

        public object bitwiseAnd(object lhs, object rhs) => Convert(lhs) & Convert(rhs);
        public object bitwiseOr(object lhs, object rhs) => Convert(lhs) | Convert(rhs);
        public object bitwiseXOr(object lhs, object rhs) => Convert(lhs) ^ Convert(rhs);
        public object bitwiseLeftShift(object lhs, object rhs) => Convert(lhs) << Convert(rhs);
        public object bitwiseRightShift(object lhs, object rhs) => Convert(lhs) >> Convert(rhs);
        public object bitwiseNot(object target) => ~Convert(target);
    }

    public class DynamicInt : IDynamicNumber
    {
        public static DynamicInt Instance = new DynamicInt();
        public Type Type => typeof(int);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Convert(object value) => System.Convert.ToInt32(this.ParseString(value) ?? value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object ConvertFrom(object value) => this.ParseString(value) 
            ?? System.Convert.ToInt32(value);

        public bool TryParse(string str, out object result)
        {
            if (int.TryParse(str, out int value))
            {
                result = value;
                return true;
            }
            result = null;
            return false;
        }

        public string ToString(object value) => Convert(value).ToString();
        public object DefaultValue => default(int);

        public object add(object lhs, object rhs) => Convert(lhs) + Convert(rhs);
        public object sub(object lhs, object rhs) => Convert(lhs) - Convert(rhs);
        public object mul(object lhs, object rhs) => Convert(lhs) * Convert(rhs);
        public object div(object lhs, object rhs) => Convert(lhs) / Convert(rhs);
        public object mod(object lhs, object rhs) => Convert(lhs) % Convert(rhs);
        public object min(object lhs, object rhs) => Math.Min(Convert(lhs), Convert(rhs));
        public object max(object lhs, object rhs) => Math.Max(Convert(lhs), Convert(rhs));
        public object pow(object lhs, object rhs) => Math.Pow(Convert(lhs), Convert(rhs));
        public object log(object lhs, object rhs) => Math.Log(Convert(lhs), Convert(rhs));
        public int compareTo(object lhs, object rhs) => Convert(lhs).CompareTo(Convert(rhs));

        public object bitwiseAnd(object lhs, object rhs) => Convert(lhs) & Convert(rhs);
        public object bitwiseOr(object lhs, object rhs) => Convert(lhs) | Convert(rhs);
        public object bitwiseXOr(object lhs, object rhs) => Convert(lhs) ^ Convert(rhs);
        public object bitwiseLeftShift(object lhs, object rhs) => Convert(lhs) << Convert(rhs);
        public object bitwiseRightShift(object lhs, object rhs) => Convert(lhs) >> Convert(rhs);
        public object bitwiseNot(object target) => ~Convert(target);
    }

    public class DynamicUInt : IDynamicNumber
    {
        public static DynamicUInt Instance = new DynamicUInt();
        public Type Type => typeof(uint);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint Convert(object value) => System.Convert.ToUInt32(this.ParseString(value) ?? value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object ConvertFrom(object value) => this.ParseString(value)
            ?? System.Convert.ToUInt32(value);

        public bool TryParse(string str, out object result)
        {
            if (uint.TryParse(str, out uint value))
            {
                result = value;
                return true;
            }
            result = null;
            return false;
        }

        public string ToString(object value) => Convert(value).ToString();
        public object DefaultValue => default(uint);

        public object add(object lhs, object rhs) => Convert(lhs) + Convert(rhs);
        public object sub(object lhs, object rhs) => Convert(lhs) - Convert(rhs);
        public object mul(object lhs, object rhs) => Convert(lhs) * Convert(rhs);
        public object div(object lhs, object rhs) => Convert(lhs) / Convert(rhs);
        public object mod(object lhs, object rhs) => Convert(lhs) % Convert(rhs);
        public object min(object lhs, object rhs) => Math.Min(Convert(lhs), Convert(rhs));
        public object max(object lhs, object rhs) => Math.Max(Convert(lhs), Convert(rhs));
        public object pow(object lhs, object rhs) => Math.Pow(Convert(lhs), Convert(rhs));
        public object log(object lhs, object rhs) => Math.Log(Convert(lhs), Convert(rhs));
        public int compareTo(object lhs, object rhs) => Convert(lhs).CompareTo(Convert(rhs));

        public object bitwiseAnd(object lhs, object rhs) => Convert(lhs) & Convert(rhs);
        public object bitwiseOr(object lhs, object rhs) => Convert(lhs) | Convert(rhs);
        public object bitwiseXOr(object lhs, object rhs) => Convert(lhs) ^ Convert(rhs);
        public object bitwiseLeftShift(object lhs, object rhs) => Convert(lhs) << (int)Convert(rhs);
        public object bitwiseRightShift(object lhs, object rhs) => Convert(lhs) >> (int)Convert(rhs);
        public object bitwiseNot(object target) => ~Convert(target);
    }

    public class DynamicLong : IDynamicNumber
    {
        public static DynamicLong Instance = new DynamicLong();
        public Type Type => typeof(long);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long Convert(object value) => System.Convert.ToInt64(this.ParseString(value) ?? value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object ConvertFrom(object value) => this.ParseString(value)
            ?? System.Convert.ToInt64(value);

        public bool TryParse(string str, out object result)
        {
            if (long.TryParse(str, out long value))
            {
                result = value;
                return true;
            }
            result = null;
            return false;
        }

        public string ToString(object value) => Convert(value).ToString();
        public object DefaultValue => default(long);

        public object add(object lhs, object rhs) => Convert(lhs) + Convert(rhs);
        public object sub(object lhs, object rhs) => Convert(lhs) - Convert(rhs);
        public object mul(object lhs, object rhs) => Convert(lhs) * Convert(rhs);
        public object div(object lhs, object rhs) => Convert(lhs) / Convert(rhs);
        public object mod(object lhs, object rhs) => Convert(lhs) % Convert(rhs);
        public object min(object lhs, object rhs) => Math.Min(Convert(lhs), Convert(rhs));
        public object max(object lhs, object rhs) => Math.Max(Convert(lhs), Convert(rhs));
        public object pow(object lhs, object rhs) => Math.Pow(Convert(lhs), Convert(rhs));
        public object log(object lhs, object rhs) => Math.Log(Convert(lhs), Convert(rhs));
        public int compareTo(object lhs, object rhs) => Convert(lhs).CompareTo(Convert(rhs));

        public object bitwiseAnd(object lhs, object rhs) => Convert(lhs) & Convert(rhs);
        public object bitwiseOr(object lhs, object rhs) => Convert(lhs) | Convert(rhs);
        public object bitwiseXOr(object lhs, object rhs) => Convert(lhs) ^ Convert(rhs);
        public object bitwiseLeftShift(object lhs, object rhs) => Convert(lhs) << (int)Convert(rhs);
        public object bitwiseRightShift(object lhs, object rhs) => Convert(lhs) >> (int)Convert(rhs);
        public object bitwiseNot(object target) => ~Convert(target);
    }

    public class DynamicULong : IDynamicNumber
    {
        public static DynamicULong Instance = new DynamicULong();
        public Type Type => typeof(ulong);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong Convert(object value) => System.Convert.ToUInt64(this.ParseString(value) ?? value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object ConvertFrom(object value) => this.ParseString(value)
            ?? System.Convert.ToUInt64(value);

        public bool TryParse(string str, out object result)
        {
            if (ulong.TryParse(str, out ulong value))
            {
                result = value;
                return true;
            }
            result = null;
            return false;
        }

        public string ToString(object value) => Convert(value).ToString();
        public object DefaultValue => default(ulong);

        public object add(object lhs, object rhs) => Convert(lhs) + Convert(rhs);
        public object sub(object lhs, object rhs) => Convert(lhs) - Convert(rhs);
        public object mul(object lhs, object rhs) => Convert(lhs) * Convert(rhs);
        public object div(object lhs, object rhs) => Convert(lhs) / Convert(rhs);
        public object mod(object lhs, object rhs) => Convert(lhs) % Convert(rhs);
        public object min(object lhs, object rhs) => Math.Min(Convert(lhs), Convert(rhs));
        public object max(object lhs, object rhs) => Math.Max(Convert(lhs), Convert(rhs));
        public object pow(object lhs, object rhs) => Math.Pow(Convert(lhs), Convert(rhs));
        public object log(object lhs, object rhs) => Math.Log(Convert(lhs), Convert(rhs));
        public int compareTo(object lhs, object rhs) => Convert(lhs).CompareTo(Convert(rhs));

        public object bitwiseAnd(object lhs, object rhs) => Convert(lhs) & Convert(rhs);
        public object bitwiseOr(object lhs, object rhs) => Convert(lhs) | Convert(rhs);
        public object bitwiseXOr(object lhs, object rhs) => Convert(lhs) ^ Convert(rhs);
        public object bitwiseLeftShift(object lhs, object rhs) => Convert(lhs) << (int)Convert(rhs);
        public object bitwiseRightShift(object lhs, object rhs) => Convert(lhs) >> (int)Convert(rhs);
        public object bitwiseNot(object target) => ~Convert(target);
    }

    public class DynamicFloat : IDynamicNumber
    {
        public static DynamicFloat Instance = new DynamicFloat();
        public Type Type => typeof(float);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Convert(object value) => System.Convert.ToSingle(this.ParseString(value) ?? value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object ConvertFrom(object value) => this.ParseString(value)
            ?? System.Convert.ToSingle(value);

        public bool TryParse(string str, out object result)
        {
            if (str.AsSpan().TryParseFloat(out float value))
            {
                result = value;
                return true;
            }
            result = null;
            return false;
        }

        public string ToString(object value) => Convert(value).ToString("r", CultureInfo.InvariantCulture);
        public object DefaultValue => default(float);

        public object add(object lhs, object rhs) => Convert(lhs) + Convert(rhs);
        public object sub(object lhs, object rhs) => Convert(lhs) - Convert(rhs);
        public object mul(object lhs, object rhs) => Convert(lhs) * Convert(rhs);
        public object div(object lhs, object rhs) => Convert(lhs) / Convert(rhs);
        public object mod(object lhs, object rhs) => Convert(lhs) % Convert(rhs);
        public object min(object lhs, object rhs) => Math.Min(Convert(lhs), Convert(rhs));
        public object max(object lhs, object rhs) => Math.Max(Convert(lhs), Convert(rhs));
        public object pow(object lhs, object rhs) => Math.Pow(Convert(lhs), Convert(rhs));
        public object log(object lhs, object rhs) => Math.Log(Convert(lhs), Convert(rhs));
        public int compareTo(object lhs, object rhs) => Convert(lhs).CompareTo(Convert(rhs));

        public object bitwiseAnd(object lhs, object rhs) => throw new NotSupportedException("Bitwise operators only supported on integer types");
        public object bitwiseOr(object lhs, object rhs) => throw new NotSupportedException("Bitwise operators only supported on integer types");
        public object bitwiseXOr(object lhs, object rhs) => throw new NotSupportedException("Bitwise operators only supported on integer types");
        public object bitwiseLeftShift(object lhs, object rhs) => throw new NotSupportedException("Bitwise operators only supported on integer types");
        public object bitwiseRightShift(object lhs, object rhs) => throw new NotSupportedException("Bitwise operators only supported on integer types");
        public object bitwiseNot(object target) => throw new NotSupportedException("Bitwise operators only supported on integer types");
    }

    public class DynamicDouble : IDynamicNumber
    {
        public static DynamicDouble Instance = new DynamicDouble();
        public Type Type => typeof(double);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Convert(object value) => System.Convert.ToDouble(this.ParseString(value) ?? value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object ConvertFrom(object value) => this.ParseString(value)
            ?? System.Convert.ToDouble(value);

        public bool TryParse(string str, out object result)
        {
            if (str.AsSpan().TryParseDouble(out double value))
            {
                result = value;
                return true;
            }
            result = null;
            return false;
        }

        public string ToString(object value) => Convert(value).ToString("r", CultureInfo.InvariantCulture);
        public object DefaultValue => default(double);

        public object add(object lhs, object rhs) => Convert(lhs) + Convert(rhs);
        public object sub(object lhs, object rhs) => Convert(lhs) - Convert(rhs);
        public object mul(object lhs, object rhs) => Convert(lhs) * Convert(rhs);
        public object div(object lhs, object rhs) => Convert(lhs) / Convert(rhs);
        public object mod(object lhs, object rhs) => Convert(lhs) % Convert(rhs);
        public object min(object lhs, object rhs) => Math.Min(Convert(lhs), Convert(rhs));
        public object max(object lhs, object rhs) => Math.Max(Convert(lhs), Convert(rhs));
        public object pow(object lhs, object rhs) => Math.Pow(Convert(lhs), Convert(rhs));
        public object log(object lhs, object rhs) => Math.Log(Convert(lhs), Convert(rhs));
        public int compareTo(object lhs, object rhs) => Convert(lhs).CompareTo(Convert(rhs));

        public object bitwiseAnd(object lhs, object rhs) => throw new NotSupportedException("Bitwise operators only supported on integer types");
        public object bitwiseOr(object lhs, object rhs) => throw new NotSupportedException("Bitwise operators only supported on integer types");
        public object bitwiseXOr(object lhs, object rhs) => throw new NotSupportedException("Bitwise operators only supported on integer types");
        public object bitwiseLeftShift(object lhs, object rhs) => throw new NotSupportedException("Bitwise operators only supported on integer types");
        public object bitwiseRightShift(object lhs, object rhs) => throw new NotSupportedException("Bitwise operators only supported on integer types");
        public object bitwiseNot(object target) => throw new NotSupportedException("Bitwise operators only supported on integer types");
    }

    public class DynamicDecimal : IDynamicNumber
    {
        public static DynamicDecimal Instance = new DynamicDecimal();
        public Type Type => typeof(decimal);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public decimal Convert(object value) => System.Convert.ToDecimal(this.ParseString(value) ?? value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object ConvertFrom(object value) => this.ParseString(value)
            ?? System.Convert.ToDecimal(value);

        public bool TryParse(string str, out object result)
        {
            if (str.AsSpan().TryParseDecimal(out decimal value))
            {
                result = value;
                return true;
            }
            result = null;
            return false;
        }

        public string ToString(object value) => Convert(value).ToString(CultureInfo.InvariantCulture);
        public object DefaultValue => default(decimal);

        public object add(object lhs, object rhs) => Convert(lhs) + Convert(rhs);
        public object sub(object lhs, object rhs) => Convert(lhs) - Convert(rhs);
        public object mul(object lhs, object rhs) => Convert(lhs) * Convert(rhs);
        public object div(object lhs, object rhs) => Convert(lhs) / Convert(rhs);
        public object mod(object lhs, object rhs) => Convert(lhs) % Convert(rhs);
        public object min(object lhs, object rhs) => Math.Min(Convert(lhs), Convert(rhs));
        public object max(object lhs, object rhs) => Math.Max(Convert(lhs), Convert(rhs));
        public object pow(object lhs, object rhs) => Math.Pow((double) Convert(lhs), (double) Convert(rhs));
        public object log(object lhs, object rhs) => Math.Log((double) Convert(lhs), (double) Convert(rhs));
        public int compareTo(object lhs, object rhs) => Convert(lhs).CompareTo(Convert(rhs));

        public object bitwiseAnd(object lhs, object rhs) => throw new NotSupportedException("Bitwise operators only supported on integer types");
        public object bitwiseOr(object lhs, object rhs) => throw new NotSupportedException("Bitwise operators only supported on integer types");
        public object bitwiseXOr(object lhs, object rhs) => throw new NotSupportedException("Bitwise operators only supported on integer types");
        public object bitwiseLeftShift(object lhs, object rhs) => throw new NotSupportedException("Bitwise operators only supported on integer types");
        public object bitwiseRightShift(object lhs, object rhs) => throw new NotSupportedException("Bitwise operators only supported on integer types");
        public object bitwiseNot(object target) => throw new NotSupportedException("Bitwise operators only supported on integer types");
    }

    public static class DynamicNumber
    {
        static readonly Dictionary<int, IDynamicNumber> RankNumbers = new Dictionary<int, IDynamicNumber>
        {
            {1, DynamicSByte.Instance},
            {2, DynamicByte.Instance},
            {3, DynamicShort.Instance},
            {4, DynamicUShort.Instance},
            {5, DynamicInt.Instance},
            {6, DynamicUInt.Instance},
            {7, DynamicLong.Instance},
            {8, DynamicULong.Instance},
            {9, DynamicFloat.Instance},
            {10, DynamicDouble.Instance},
            {11, DynamicDecimal.Instance},
        };

        public static bool IsNumber(Type type) => TryGetRanking(type, out _);

        public static bool TryGetRanking(Type type, out int ranking)
        {
            ranking = -1;
            switch (type.GetTypeCode())
            {
                case TypeCode.SByte:
                    ranking = 1;
                    break;
                case TypeCode.Byte:
                    ranking = 2;
                    break;
                case TypeCode.Int16:
                    ranking = 3;
                    break;
                case TypeCode.UInt16:
                    ranking = 4;
                    break;
                case TypeCode.Char:
                case TypeCode.Int32:
                    ranking = 5;
                    break;
                case TypeCode.UInt32:
                    ranking = 6;
                    break;
                case TypeCode.Int64:
                    ranking = 7;
                    break;
                case TypeCode.UInt64:
                    ranking = 8;
                    break;
                case TypeCode.Single:
                    ranking = 9;
                    break;
                case TypeCode.Double:
                    ranking = 10;
                    break;
                case TypeCode.Decimal:
                    ranking = 11;
                    break;
            }

            return ranking > 0;
        }

        public static IDynamicNumber GetNumber(Type type)
        {
            if (!TryGetRanking(type, out int objIndex))
                return null;

            var maxNumber = RankNumbers[objIndex];
            return maxNumber;
        }

        public static IDynamicNumber Get(object obj)
        {
            if (obj == null)
                return null;

            if (obj is string lhsString && !TryParse(lhsString, out obj))
                return null;

            return TryGetRanking(obj.GetType(), out int lhsRanking)
                ? RankNumbers[lhsRanking]
                : null;
        }
        
        public static IDynamicNumber GetNumber(object lhs, object rhs)
        {
            if (lhs == null || rhs == null)
                return null;

            if (lhs is string lhsString && !TryParse(lhsString, out lhs))
                return null;

            if (rhs is string rhsString && !TryParse(rhsString, out rhs))
                return null;

            if (!TryGetRanking(lhs.GetType(), out int lhsRanking) || !TryGetRanking(rhs.GetType(), out int rhsRanking))
                return null;

            var maxRanking = Math.Max(lhsRanking, rhsRanking);
            var maxNumber = RankNumbers[maxRanking];
            return maxNumber;
        }

        public static IDynamicNumber AssertNumbers(string name, object lhs, object rhs)
        {
            var number = GetNumber(lhs, rhs);
            if (number == null)
            {
                throw new ArgumentException($"Invalid numbers passed to {name}: " +
                                            $"({lhs?.GetType().Name ?? "null"} '{lhs?.ToString().SubstringWithEllipsis(0, 100)}', " +
                                            $"{rhs?.GetType().Name ?? "null"} '{rhs?.ToString().SubstringWithEllipsis(0, 100)}')");
            }

            return number;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object Add(object lhs, object rhs) => AssertNumbers(nameof(Add), lhs, rhs).add(lhs, rhs);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object Sub(object lhs, object rhs) => AssertNumbers(nameof(Subtract), lhs, rhs).sub(lhs, rhs);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object Subtract(object lhs, object rhs) => AssertNumbers(nameof(Subtract), lhs, rhs).sub(lhs, rhs);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object Mul(object lhs, object rhs) => AssertNumbers(nameof(Multiply), lhs, rhs).mul(lhs, rhs);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object Multiply(object lhs, object rhs) => AssertNumbers(nameof(Multiply), lhs, rhs).mul(lhs, rhs);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object Div(object lhs, object rhs) => AssertNumbers(nameof(Divide), lhs, rhs).div(lhs, rhs);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object Divide(object lhs, object rhs) => AssertNumbers(nameof(Divide), lhs, rhs).div(lhs, rhs);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object Mod(object lhs, object rhs) => AssertNumbers(nameof(Mod), lhs, rhs).mod(lhs, rhs);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object Min(object lhs, object rhs) => AssertNumbers(nameof(Min), lhs, rhs).min(lhs, rhs);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object Max(object lhs, object rhs) => AssertNumbers(nameof(Max), lhs, rhs).max(lhs, rhs);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object Pow(object lhs, object rhs) => AssertNumbers(nameof(Pow), lhs, rhs).pow(lhs, rhs);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object Log(object lhs, object rhs) => AssertNumbers(nameof(Log), lhs, rhs).log(lhs, rhs);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CompareTo(object lhs, object rhs) => AssertNumbers(nameof(CompareTo), lhs, rhs).compareTo(lhs, rhs);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object BitwiseAnd(object lhs, object rhs) => AssertNumbers(nameof(BitwiseAnd), lhs, rhs).bitwiseAnd(lhs, rhs);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object BitwiseOr(object lhs, object rhs) => AssertNumbers(nameof(BitwiseOr), lhs, rhs).bitwiseOr(lhs, rhs);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object BitwiseXOr(object lhs, object rhs) => AssertNumbers(nameof(BitwiseXOr), lhs, rhs).bitwiseXOr(lhs, rhs);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object BitwiseLeftShift(object lhs, object rhs) => AssertNumbers(nameof(BitwiseLeftShift), lhs, rhs).bitwiseLeftShift(lhs, rhs);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object BitwiseRightShift(object lhs, object rhs) => AssertNumbers(nameof(BitwiseRightShift), lhs, rhs).bitwiseRightShift(lhs, rhs);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object BitwiseNot(object lhs) => Get(lhs).bitwiseNot(lhs);

        public static bool TryParse(string strValue, out object result)
        {
            if (JsConfig.TryParseIntoBestFit)
                return TryParseIntoBestFit(strValue, out result);

            result = null;
            if (!(strValue?.Length > 0))
                return false;

            if (strValue.Length == 1)
            {
                int singleDigit = strValue[0];
                if (singleDigit >= '0' && singleDigit <= '9')
                {
                    result = singleDigit - 48; // 0 
                    return true;
                }
                return false;
            }

            var hasDecimal = strValue.IndexOf('.') >= 0;
            if (!hasDecimal)
            {
                if (int.TryParse(strValue, out int intValue))
                {
                    result = intValue;
                    return true;
                }
                if (long.TryParse(strValue, out long longValue))
                {
                    result = longValue;
                    return true;
                }
                if (ulong.TryParse(strValue, out ulong ulongValue))
                {
                    result = ulongValue;
                    return true;
                }
            }

            var spanValue = strValue.AsSpan();
            if (spanValue.TryParseDouble(out double doubleValue))
            {
                result = doubleValue;
                return true;
            }

            if (spanValue.TryParseDecimal(out decimal decimalValue))
            {
                result = decimalValue;
                return true;
            }

            return false;
        }

        public static bool TryParseIntoBestFit(string strValue, out object result)
        {
            result = null;
            if (!(strValue?.Length > 0))
                return false;

            var segValue = strValue.AsSpan();
            result = segValue.ParseNumber(bestFit:true);
            return result != null;
        }
    }

    internal static class DynamicNumberExtensions
    {
        internal static object ParseString(this IDynamicNumber number, object value)
        {
            if (value is string s)
                return number.TryParse(s, out object x) ? x : null;

            if (value is char c)
                return number.TryParse(c.ToString(), out object x) ? x : null;

            return null;
        }

    }
}
