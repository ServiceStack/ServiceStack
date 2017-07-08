using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using ServiceStack.Configuration;

namespace ServiceStack.Templates
{
    public class SingletonFactoryResolver : IResolver, IRuntimeResolver
    {
        private readonly TemplatePagesContext context;

        public SingletonFactoryResolver(TemplatePagesContext context)
        {
            this.context = context;
        }

        public readonly ConcurrentDictionary<Type, object> Cache = new ConcurrentDictionary<Type, object>();

        public T TryResolve<T>()
        {
            return (T) Cache.GetOrAdd(typeof(T), TryResolve);
        }

        public object TryResolve(Type type)
        {
            return context.Factory.TryGetValue(type, out Func<object> fn)
                ? fn()
                : !(type.IsAbstract() || type.IsInterface()) 
                    ? type.CreateInstance()
                    : null;
        }
    }
}