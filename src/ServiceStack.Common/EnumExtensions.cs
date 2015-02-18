using System;
using System.Collections.Generic;

namespace ServiceStack
{
    public static class EnumExtensions
    {
        /// <summary>
        /// Gets the textual description of the enum if it has one. e.g.
        /// 
        /// <code>
        /// enum UserColors
        /// {
        ///     [Description("Bright Red")]
        ///     BrightRed
        /// }
        /// UserColors.BrightRed.ToDescription();
        /// </code>
        /// </summary>
        /// <param name="enum"></param>
        /// <returns></returns>
#if !NETFX_CORE
        public static string ToDescription(this Enum @enum) 
        {
            var type = @enum.GetType();

            var memInfo = type.GetMember(@enum.ToString());
            if (memInfo.Length > 0)
            {
                var description = memInfo[0].GetDescription();

                if (description != null)
                    return description;
            }

            return @enum.ToString();
        }
#endif

        public static List<string> ToList(this Enum @enum)
        {
#if !(SL54 || WP)
            return new List<string>(Enum.GetNames(@enum.GetType()));
#else
            return @enum.GetType().GetFields(BindingFlags.Static | BindingFlags.Public).Select(fi => fi.Name).ToList();
#endif
        }

        public static TypeCode GetTypeCode(this Enum @enum)
        {
            return Type.GetTypeCode(Enum.GetUnderlyingType(@enum.GetType()));
        }
        
        public static bool Has<T>(this Enum @enum, T value)
        {
            var typeCode = @enum.GetTypeCode();
            switch (typeCode)
            {
                case TypeCode.Byte:
                    return (((byte)(object)@enum & (byte)(object)value) == (byte)(object)value);
                case TypeCode.Int16:
                    return (((short)(object)@enum & (short)(object)value) == (short)(object)value);
                case TypeCode.Int32:
                    return (((int)(object)@enum & (int)(object)value) == (int)(object)value);
                case TypeCode.Int64:
                    return (((long)(object)@enum & (long)(object)value) == (long)(object)value);
                default:
                    throw new NotSupportedException("Enums of type {0}".Fmt(@enum.GetType().Name));
            }
        }

        public static bool Is<T>(this Enum @enum, T value)
        {
            var typeCode = @enum.GetTypeCode();
            switch (typeCode)
            {
                case TypeCode.Byte:
                    return (byte)(object)@enum == (byte)(object)value;
                case TypeCode.Int16:
                    return (short)(object)@enum == (short)(object)value;
                case TypeCode.Int32:
                    return (int)(object)@enum == (int)(object)value;
                case TypeCode.Int64:
                    return (long)(object)@enum == (long)(object)value;
                default:
                    throw new NotSupportedException("Enums of type {0}".Fmt(@enum.GetType().Name));
            }
        }

        public static T Add<T>(this Enum @enum, T value)
        {
            var typeCode = @enum.GetTypeCode();
            switch (typeCode)
            {
                case TypeCode.Byte:
                    return (T)(object)(((byte)(object)@enum | (byte)(object)value));
                case TypeCode.Int16:
                    return (T)(object)(((short)(object)@enum | (short)(object)value));
                case TypeCode.Int32:
                    return (T)(object)(((int)(object)@enum | (int)(object)value));
                case TypeCode.Int64:
                    return (T)(object)(((long)(object)@enum | (long)(object)value));
                default:
                    throw new NotSupportedException("Enums of type {0}".Fmt(@enum.GetType().Name));
            }
        }

        public static T Remove<T>(this Enum @enum, T value)
        {
            var typeCode = @enum.GetTypeCode();
            switch (typeCode)
            {
                case TypeCode.Byte:
                    return (T)(object)(((byte)(object)@enum & ~(byte)(object)value));
                case TypeCode.Int16:
                    return (T)(object)(((short)(object)@enum & ~(short)(object)value));
                case TypeCode.Int32:
                    return (T)(object)(((int)(object)@enum & ~(int)(object)value));
                case TypeCode.Int64:
                    return (T)(object)(((long)(object)@enum & ~(long)(object)value));
                default:
                    throw new NotSupportedException("Enums of type {0}".Fmt(@enum.GetType().Name));
            }
        }

    }

}