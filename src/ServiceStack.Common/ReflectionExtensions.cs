using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using ServiceStack.Text;
using ServiceStack.Utils;

namespace ServiceStack
{
    public static class ReflectionExtensions
    {
        public static To PopulateWith<To, From>(this To to, From from)
        {
            return ReflectionUtils.PopulateObject(to, from);
        }

        public static To PopulateWithNonDefaultValues<To, From>(this To to, From from)
        {
            return ReflectionUtils.PopulateWithNonDefaultValues(to, from);
        }

        public static To PopulateFromPropertiesWithAttribute<To, From, TAttr>(this To to, From from)
        {
            return ReflectionUtils.PopulateFromPropertiesWithAttribute(to, from, typeof(TAttr));
        }

        public static To PopulateFromPropertiesWithAttribute<To, From>(this To to, From from, Type attrType)
        {
            return ReflectionUtils.PopulateFromPropertiesWithAttribute(to, from, attrType);
        }

        public static T ConvertTo<T>(this object from)
        {
            var to = typeof(T).CreateInstance<T>();
            return to.PopulateWith(from);
        }

        private static readonly Dictionary<Type, List<string>> TypePropertyNamesMap = new Dictionary<Type, List<string>>();

        public static List<string> GetPropertyNames(this Type type)
        {
            lock (TypePropertyNamesMap)
            {
                List<string> propertyNames;
                if (!TypePropertyNamesMap.TryGetValue(type, out propertyNames))
                {
                    propertyNames = type.Properties().SafeConvertAll(x => x.Name);
                    TypePropertyNamesMap[type] = propertyNames;
                }
                return propertyNames;
            }
        }

        public static List<T> ToAttributes<T>(this Type type) where T : Attribute
        {
            return type.CustomAttributes(typeof(T)).Map(x => (T)x);
        }

#if !SILVERLIGHT
        public static string GetAssemblyPath(this Type source)
        {
            var assemblyUri =
                new Uri(source.Assembly.EscapedCodeBase);

            return assemblyUri.LocalPath;
        }
#endif

        public static bool IsDebugBuild(this Assembly assembly)
        {
#if NETFX_CORE
            return assembly.GetCustomAttributes()
                .OfType<DebuggableAttribute>()
                .Any();
#elif WINDOWS_PHONE || SILVERLIGHT
            return assembly.GetCustomAttributes(false)
                .OfType<DebuggableAttribute>()
                .Any();
#else
            return assembly.GetCustomAttributes(false)
                .OfType<DebuggableAttribute>()
                .Select(attr => attr.IsJITTrackingEnabled)
                .FirstOrDefault();
#endif
        }
    }
}
