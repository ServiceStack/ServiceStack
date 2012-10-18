using System;
using System.Reflection;

using Proxy = ServiceStack.Common.ReflectionExtensions;

namespace ServiceStack.Common.Extensions
{
    public static class ReflectionExtensions
    {
        public static To PopulateWith<To, From>(this To to, From from)
        {
            return Proxy.PopulateWith(to, from);
        }

        public static To PopulateWithNonDefaultValues<To, From>(this To to, From from)
        {
            return Proxy.PopulateWithNonDefaultValues(to, from);
        }

        public static To PopulateFromPropertiesWithAttribute<To, From, TAttr>(this To to, From from)
        {
            return Proxy.PopulateFromPropertiesWithAttribute<To, From, TAttr>(to, from);
        }

        public static T TranslateTo<T>(this object from)
            where T : new()
        {
            return Proxy.TranslateTo<T>(from);
        }

        public static TAttribute FirstAttribute<TAttribute>(this Type type)
        {
            return Proxy.FirstAttribute<TAttribute>(type);
        }

        public static TAttribute FirstAttribute<TAttribute>(this Type type, bool inherit)
        {
            return Proxy.FirstAttribute<TAttribute>(type, inherit);
        }

        public static TAttribute FirstAttribute<TAttribute>(this PropertyInfo propertyInfo)
        {
            return Proxy.FirstAttribute<TAttribute>(propertyInfo);
        }

        public static TAttribute FirstAttribute<TAttribute>(this PropertyInfo propertyInfo, bool inherit)
        {
            return Proxy.FirstAttribute<TAttribute>(propertyInfo, inherit);
        }

        public static bool IsGenericType(this Type type)
        {
            return Proxy.IsGenericType(type);
        }

        public static Type FirstGenericTypeDefinition(this Type type)
        {
            return Proxy.FirstGenericTypeDefinition(type);
        }
    }
}
