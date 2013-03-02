using System;
using System.Reflection;

using Proxy = ServiceStack.Common.ReflectionExtensions;
using ProxyText = ServiceStack.Text.PlatformExtensions;

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
            where TAttribute : Attribute
        {
            return ProxyText.FirstAttribute<TAttribute>(type);
        }

        public static TAttribute FirstAttribute<TAttribute>(this Type type, bool inherit)
            where TAttribute : Attribute
        {
            return ProxyText.FirstAttribute<TAttribute>(type, inherit);
        }

        public static TAttribute FirstAttribute<TAttribute>(this PropertyInfo propertyInfo)
            where TAttribute : Attribute
        {
            return ProxyText.FirstAttribute<TAttribute>(propertyInfo);
        }

        public static TAttribute FirstAttribute<TAttribute>(this PropertyInfo propertyInfo, bool inherit)
            where TAttribute : Attribute
        {
            return ProxyText.FirstAttribute<TAttribute>(propertyInfo, inherit);
        }

        public static Type FirstGenericTypeDefinition(this Type type)
        {
            return ProxyText.FirstGenericTypeDefinition(type);
        }
    }
}
