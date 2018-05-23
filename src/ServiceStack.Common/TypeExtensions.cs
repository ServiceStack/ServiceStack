using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace ServiceStack
{
    public delegate object ObjectActivator(params object[] args);

    public delegate object MethodInvoker(object instance, params object[] args);

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
            if (type.BaseType != null)
            {
                if (!refTypes.Contains(type.BaseType))
                {
                    refTypes.Add(type.BaseType);
                    AddReferencedTypes(type.BaseType, refTypes);
                }

                if (!type.BaseType.GetGenericArguments().IsEmpty())
                {
                    foreach (var arg in type.BaseType.GetGenericArguments())
                    {
                        if (!refTypes.Contains(arg))
                        {
                            refTypes.Add(arg);
                            AddReferencedTypes(arg, refTypes);
                        }
                    }
                }
            }

            var properties = type.GetProperties();
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

            var convertFromMethod = typeof(TypeExtensions).GetStaticMethod(nameof(ConvertFromObject));

            for (int i = 0; i < pi.Length; i++)
            {
                var index = Expression.Constant(i);
                var paramType = pi[i].ParameterType;
                var paramAccessorExp = Expression.ArrayIndex(paramArgs, index);
                var paramCastExp = Expression.Convert(paramAccessorExp, paramType);
                var convertParam = convertFromMethod.MakeGenericMethod(paramType);
                exprArgs[i] = Expression.Call(convertParam, paramAccessorExp);
            }

            var newExp = Expression.New(ctor, exprArgs);
            var lambda = Expression.Lambda(typeof(ObjectActivator), newExp, paramArgs);

            var ctorFn = (ObjectActivator)lambda.Compile();
            return ctorFn;
        }

        static Dictionary<ConstructorInfo, ObjectActivator> activatorCache =
            new Dictionary<ConstructorInfo, ObjectActivator>();

        public static ObjectActivator GetActivator(this ConstructorInfo ctor)
        {
            if (activatorCache.TryGetValue(ctor, out var fn))
                return fn;

            fn = GetActivatorToCache(ctor);

            Dictionary<ConstructorInfo, ObjectActivator> snapshot, newCache;
            do
            {
                snapshot = activatorCache;
                newCache = new Dictionary<ConstructorInfo, ObjectActivator>(activatorCache) { [ctor] = fn };

            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref activatorCache, newCache, snapshot), snapshot));

            return fn;
        }

        public static MethodInvoker GetInvokerToCache(MethodInfo method)
        {
            var pi = method.GetParameters();
            var paramInstance = Expression.Parameter(typeof(object), "instance");
            var paramArgs = Expression.Parameter(typeof(object[]), "args");

            var convertFromMethod = typeof(TypeExtensions).GetStaticMethod(nameof(ConvertFromObject));

            var exprArgs = new Expression[pi.Length];
            for (int i = 0; i < pi.Length; i++)
            {
                var index = Expression.Constant(i);
                var paramType = pi[i].ParameterType;
                var paramAccessorExp = Expression.ArrayIndex(paramArgs, index);
                var convertParam = convertFromMethod.MakeGenericMethod(paramType);
                exprArgs[i] = Expression.Call(convertParam, paramAccessorExp);
            }
            
            var methodCall = Expression.Call(Expression.TypeAs(paramInstance, method.DeclaringType), method, exprArgs);

            var convertToMethod = typeof(TypeExtensions).GetStaticMethod(nameof(ConvertToObject));
            var convertReturn = convertToMethod.MakeGenericMethod(method.ReturnType);
            
            var lambda = Expression.Lambda(typeof(MethodInvoker), 
                Expression.Call(convertReturn, methodCall), 
                paramInstance, 
                paramArgs);

            var fn = (MethodInvoker)lambda.Compile();
            return fn;
        }
        
        static Dictionary<MethodInfo, MethodInvoker> invokerCache =
            new Dictionary<MethodInfo, MethodInvoker>();

        public static MethodInvoker GetInvoker(this MethodInfo method)
        {
            if (invokerCache.TryGetValue(method, out var fn))
                return fn;

            fn = GetInvokerToCache(method);

            Dictionary<MethodInfo, MethodInvoker> snapshot, newCache;
            do
            {
                snapshot = invokerCache;
                newCache = new Dictionary<MethodInfo, MethodInvoker>(invokerCache) { [method] = fn };

            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref invokerCache, newCache, snapshot), snapshot));

            return fn;
        }

        public static T ConvertFromObject<T>(object value)
        {
            if (value == null)
                return default(T);
            
            if (value is T variable)
                return variable;

            if (typeof(T) == typeof(string) && value is IRawString rs)
                return (T)(object)rs.ToRawString();

            return value.ConvertTo<T>();
        }

        public static object ConvertToObject<T>(T value)
        {
            return value;
        }
    }

}