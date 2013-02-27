using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using ServiceStack.Text;

namespace ServiceStack.Common
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
            if (memInfo != null && memInfo.Length > 0)
            {
                var attrs = memInfo[0].GetCustomAttributes(
                    typeof(DescriptionAttribute),
                    false);

                if (attrs != null && attrs.Length > 0)
                    return ((DescriptionAttribute)attrs[0]).Description;
            }

            return @enum.ToString();
        }
#endif

        public static List<string> ToList(this Enum @enum)
        {
#if !(SILVERLIGHT4 || WINDOWS_PHONE)
            return new List<string>(Enum.GetNames(@enum.GetType()));
#else
            return @enum.GetType().GetFields(BindingFlags.Static | BindingFlags.Public).Select(fi => fi.Name).ToList();
#endif
        }
        
        public static bool Has<T>(this Enum @enum, T value)
        {
            var enumType = Enum.GetUnderlyingType(@enum.GetType());
            if (enumType == typeof(int))
                return (((int)(object)@enum & (int)(object)value) == (int)(object)value);
            if (enumType == typeof(long))
                return (((long)(object)@enum & (long)(object)value) == (long)(object)value);
            if (enumType == typeof(byte))
                return (((byte)(object)@enum & (byte)(object)value) == (byte)(object)value);

            throw new NotSupportedException("Enums of type {0}".Fmt(enumType.Name));
        }

        public static bool Is<T>(this Enum @enum, T value)
        {
            var enumType = Enum.GetUnderlyingType(@enum.GetType());
            if (enumType == typeof(int))
                return (int)(object)@enum == (int)(object)value;
            if (enumType == typeof(long))
                return (long)(object)@enum == (long)(object)value;
            if (enumType == typeof(byte))
                return (byte)(object)@enum == (byte)(object)value;

            throw new NotSupportedException("Enums of type {0}".Fmt(enumType.Name));
        }


        public static T Add<T>(this Enum @enum, T value)
        {
            var enumType = Enum.GetUnderlyingType(@enum.GetType());
            if (enumType == typeof(int))
                return (T)(object)(((int)(object)@enum | (int)(object)value));
            if (enumType == typeof(long))
                return (T)(object)(((long)(object)@enum | (long)(object)value));
            if (enumType == typeof(byte))
                return (T)(object)(((byte)(object)@enum | (byte)(object)value));

            throw new NotSupportedException("Enums of type {0}".Fmt(enumType.Name));
        }

        public static T Remove<T>(this Enum @enum, T value)
        {
            var enumType = Enum.GetUnderlyingType(@enum.GetType());
            if (enumType == typeof(int))
                return (T)(object)(((int)(object)@enum & ~(int)(object)value));
            if (enumType == typeof(long))
                return (T)(object)(((long)(object)@enum & ~(long)(object)value));
            if (enumType == typeof(byte))
                return (T)(object)(((byte)(object)@enum & ~(byte)(object)value));

            throw new NotSupportedException("Enums of type {0}".Fmt(enumType.Name));
        }

    }

}