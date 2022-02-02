using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Funq;
using ServiceStack.Configuration;

namespace ServiceStack.ServiceHost.Tests.TypeFactory
{
    public class FuncTypeFactory
        : ITypeFactory
    {
        private readonly Container container;
        private readonly Dictionary<Type, Func<object>> resolveFnMap = new Dictionary<Type, Func<object>>();

        public FuncTypeFactory(Container container)
        {
            this.container = container;
        }

        public object CreateInstance(IResolver resolver, Type type)
        {
            Func<object> resolveFn;

            if (!this.resolveFnMap.TryGetValue(type, out resolveFn))
            {
                var containerInstance = Expression.Constant(this.container);
                var resolveInstance = Expression.Call(containerInstance, "Resolve", new[] { type }, new Expression[0]);
                var resolveObject = Expression.Convert(resolveInstance, typeof(object));
                resolveFn = (Func<object>)Expression.Lambda(resolveObject, new ParameterExpression[0]).Compile();

                lock (this.resolveFnMap)
                {
                    this.resolveFnMap[type] = resolveFn;
                }
            }

            return resolveFn();
        }
    }
}