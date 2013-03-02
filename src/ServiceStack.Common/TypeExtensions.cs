using System;
using System.Collections.Generic;
using ServiceStack.Text;

namespace ServiceStack.Common
{
    public static class TypeExtensions
    {
        private static readonly Dictionary<Type, List<string>> TypePropertyNamesMap = new Dictionary<Type, List<string>>();

        public static List<string> GetPropertyNames(this Type type)
        {
            lock (TypePropertyNamesMap)
            {
                List<string> propertyNames;
                if (!TypePropertyNamesMap.TryGetValue(type, out propertyNames))
                {
                    propertyNames = Extensions.EnumerableExtensions.ConvertAll(type.Properties(), x => x.Name);
                    TypePropertyNamesMap[type] = propertyNames;
                }
                return propertyNames;
            }
        }

        public static List<T> ToAttributes<T>(this Type type) where T : Attribute
        {
            return type.CustomAttributes(typeof(T)).SafeConvertAll(x => (T)x);
        }

#if !SILVERLIGHT
        public static string GetAssemblyPath(this Type source)
        {
            var assemblyUri =
                new Uri(source.Assembly.EscapedCodeBase);

            return assemblyUri.LocalPath;
        }
#endif
    }
}
