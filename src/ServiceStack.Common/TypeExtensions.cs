using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace ServiceStack
{
    public delegate object ObjectActivator(params object[] args);

    public static class TypeExtensions
    {
        public static Type[] GetReferencedTypes(this Type type)
        {
            var refTypes = new HashSet<Type> { type };

            AddReferencedTypes(type, refTypes);

            return refTypes.ToArray();
        }

        public static void AddReferencedTypes(Type type, HashSet<Type> refTypes)
        {
            if (type.BaseType() != null)
            {
                if (!refTypes.Contains(type.BaseType()))
                {
                    refTypes.Add(type.BaseType());
                    AddReferencedTypes(type.BaseType(), refTypes);
                }

                if (!type.BaseType().GetGenericArguments().IsEmpty())
                {
                    foreach (var arg in type.BaseType().GetGenericArguments())
                    {
                        if (!refTypes.Contains(arg))
                        {
                            refTypes.Add(arg);
                            AddReferencedTypes(arg, refTypes);
                        }
                    }
                }
            }

            var properties = type.GetPropertyInfos();
            if (!properties.IsEmpty())
            {
                foreach (var p in properties)
                {
                    if (!refTypes.Contains(p.PropertyType))
                    {
                        refTypes.Add(p.PropertyType);
                        AddReferencedTypes(type, refTypes);
                    }

                    var args = p.PropertyType.GetGenericArguments();
                    if (!args.IsEmpty())
                    {
                        foreach (var arg in args)
                        {
                            if (!refTypes.Contains(arg))
                            {
                                refTypes.Add(arg);
                                AddReferencedTypes(arg, refTypes);
                            }
                        }
                    }
                    else if (p.PropertyType.IsArray)
                    {
                        var elType = p.PropertyType.GetElementType();
                        if (!refTypes.Contains(elType))
                        {
                            refTypes.Add(elType);
                            AddReferencedTypes(elType, refTypes);
                        }
                    }
                }
            }
        }

        public static ObjectActivator GetActivatorToCache(ConstructorInfo ctor)
        {
            var pi = ctor.GetParameters();
            var paramArgs = Expression.Parameter(typeof(object[]), "args");
            var exprArgs = new Expression[pi.Length];

            for (int i = 0; i < pi.Length; i++)
            {
                var index = Expression.Constant(i);
                var paramType = pi[i].ParameterType;
                var paramAccessorExp = Expression.ArrayIndex(paramArgs, index);
                var paramCastExp = Expression.Convert(paramAccessorExp, paramType);
                exprArgs[i] = paramCastExp;
            }

            var newExp = Expression.New(ctor, exprArgs);
            var lambda = Expression.Lambda(typeof(ObjectActivator), newExp, paramArgs);

            var ctorFn = (ObjectActivator)lambda.Compile();
            return ctorFn;
        }

        static Dictionary<ConstructorInfo, ObjectActivator> ActivatorCache =
            new Dictionary<ConstructorInfo, ObjectActivator>();

        public static ObjectActivator GetActivator(this ConstructorInfo ctor)
        {
            ObjectActivator fn;
            if (ActivatorCache.TryGetValue(ctor, out fn))
                return fn;

            fn = GetActivatorToCache(ctor);

            Dictionary<ConstructorInfo, ObjectActivator> snapshot, newCache;
            do
            {
                snapshot = ActivatorCache;
                newCache = new Dictionary<ConstructorInfo, ObjectActivator>(ActivatorCache) { [ctor] = fn };

            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref ActivatorCache, newCache, snapshot), snapshot));

            return fn;
        }

    }

}