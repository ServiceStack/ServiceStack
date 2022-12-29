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
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace ServiceStack.Text.Common
{
    internal static class ParseUtils
    {
        public static object NullValueType(Type type)
        {
            return type.GetDefaultValue();
        }

        public static object ParseObject(string value)
        {
            return value;
        }

        public static object ParseEnum(Type type, string value)
        {
            return Enum.Parse(type, value, false);
        }

        public static ParseStringDelegate GetSpecialParseMethod(Type type)
        {
            if (type == typeof(Uri))
                return x => new Uri(x.FromCsvField());

            //Warning: typeof(object).IsInstanceOfType(typeof(Type)) == True??
            if (type.IsInstanceOfType(typeof(Type)))
                return ParseType;

            if (type == typeof(Exception))
                return x => new Exception(x);

            if (type.IsInstanceOf(typeof(Exception)))
                return DeserializeTypeUtils.GetParseMethod(type);

            return null;
        }

        public static Type ParseType(string assemblyQualifiedName)
        {
            return AssemblyUtils.FindType(assemblyQualifiedName.FromCsvField());
        }

        public static object TryParseEnum(Type enumType, string str)
        {
            if (str == null)
                return null;

            if (JsConfig.TextCase == TextCase.SnakeCase)
            {
                string[] names = Enum.GetNames(enumType);
                if (Array.IndexOf(names, str) == -1)    // case sensitive ... could use Linq Contains() extension with StringComparer.InvariantCultureIgnoreCase instead for a slight penalty
                    str = str.Replace("_", "");
            }

            var enumInfo = CachedTypeInfo.Get(enumType).EnumInfo;
            return enumInfo.Parse(str);
        }
    }

}