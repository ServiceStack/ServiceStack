using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Configuration;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
    public static class TemplatePageExtensions
    {
        public static async Task<string> RenderToStringAsync(this IStreamWriterAsync writer)
        {
            using (var ms = MemoryStreamFactory.GetStream())
            {
                await writer.WriteToAsync(ms);
                ms.Position = 0;
                using (var reader = new StreamReader(ms))
                {
                    return await reader.ReadToEndAsync();
                }
            }
        }

        public static object TryResolve(this IResolver resolver, Type type)
        {
            if (resolver is IRuntimeResolver runtimeResolver)
            {
                return runtimeResolver.TryResolve(type);
            }
            
            Func<IResolver, object> fn;
            if (tryResolveCache.TryGetValue(type, out fn))
                return fn(resolver);

            var mi = resolver.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .First(x => x.Name == "TryResolve" &&
                            x.GetGenericArguments().Length == 1 &&
                            x.GetParameters().Length == 0);

            var genericMi = mi.MakeGenericMethod(type);

            var p = Expression.Parameter(typeof(IResolver), "resolver");
            fn = Expression.Lambda<Func<IResolver, object>>(
                Expression.Call(Expression.TypeAs(p, resolver.GetType()), genericMi),
                p
            ).Compile();

            Dictionary<Type, Func<IResolver, object>> snapshot, newCache;
            do
            {
                snapshot = tryResolveCache;
                newCache = new Dictionary<Type, Func<IResolver, object>>(tryResolveCache) {
                    [type] = fn
                };
            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref tryResolveCache, newCache, snapshot), snapshot));

            return fn(resolver);
        }
        
        private static Dictionary<Type, Func<IResolver,object>> tryResolveCache = new Dictionary<Type, Func<IResolver, object>>();
    }
}