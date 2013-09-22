using System;
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
