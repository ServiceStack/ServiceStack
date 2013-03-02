using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using ServiceStack.Common.Utils;

namespace ServiceStack.Common
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

        public static T TranslateTo<T>(this object from)
            where T : new()
        {
            var to = new T();
            return to.PopulateWith(from);
        }

        public static bool IsDebugBuild(this Assembly assembly)
        {
#if NETFX_CORE
            return assembly.GetCustomAttributes()
                .OfType<DebuggableAttribute>()
                .Any();
#elif WINDOWS_PHONE
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


#if FALSE && DOTNET35
//Efficient POCO Translator from: http://www.yoda.arachsys.com/csharp/miscutil/
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace MiscUtil.Reflection
{
    /// <summary>
    /// Generic class which copies to its target type from a source
    /// type specified in the Copy method. The types are specified
    /// separately to take advantage of type inference on generic
    /// method arguments.
    /// </summary>
    public static class PropertyCopy<TTarget> where TTarget : class, new()
    {
        /// <summary>
        /// Copies all readable properties from the source to a new instance
        /// of TTarget.
        /// </summary>
        public static TTarget CopyFrom<TSource>(TSource source) where TSource : class
        {
            return PropertyCopier<TSource>.Copy(source);
        }

        /// <summary>
        /// Static class to efficiently store the compiled delegate which can
        /// do the copying. We need a bit of work to ensure that exceptions are
        /// appropriately propagated, as the exception is generated at type initialization
        /// time, but we wish it to be thrown as an ArgumentException.
        /// </summary>
        private static class PropertyCopier<TSource> where TSource : class
        {
            private static readonly Func<TSource, TTarget> copier;
            private static readonly Exception initializationException;

            internal static TTarget Copy(TSource source)
            {
                if (initializationException != null)
                {
                    throw initializationException;
                }
                if (source == null)
                {
                    throw new ArgumentNullException("source");
                }
                return copier(source);
            }

            static PropertyCopier()
            {
                try
                {
                    copier = BuildCopier();
                    initializationException = null;
                }
                catch (Exception e)
                {
                    copier = null;
                    initializationException = e;
                }
            }

            private static Func<TSource, TTarget> BuildCopier()
            {
                ParameterExpression sourceParameter = Expression.Parameter(typeof(TSource), "source");
                var bindings = new List<MemberBinding>();
                foreach (PropertyInfo sourceProperty in typeof(TSource).GetProperties())
                {
                    if (!sourceProperty.CanRead)
                    {
                        continue;
                    }
                    PropertyInfo targetProperty = typeof(TTarget).GetProperty(sourceProperty.Name);
                    if (targetProperty == null)
                    {
                        throw new ArgumentException("Property " + sourceProperty.Name + " is not present and accessible in " + typeof(TTarget).FullName);
                    }
                    if (!targetProperty.CanWrite)
                    {
                        throw new ArgumentException("Property " + sourceProperty.Name + " is not writable in " + typeof(TTarget).FullName);
                    }
                    if (!targetProperty.PropertyType.IsAssignableFrom(sourceProperty.PropertyType))
                    {
                        throw new ArgumentException("Property " + sourceProperty.Name + " has an incompatible type in " + typeof(TTarget).FullName);
                    }
                    bindings.Add(Expression.Bind(targetProperty, Expression.Property(sourceParameter, sourceProperty)));
                }
                Expression initializer = Expression.MemberInit(Expression.New(typeof(TTarget)), bindings);
                return Expression.Lambda<Func<TSource,TTarget>>(initializer, sourceParameter).Compile();
            }
        }
    }
}
#endif
