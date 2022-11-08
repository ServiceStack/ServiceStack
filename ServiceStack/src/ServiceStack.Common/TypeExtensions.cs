using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace ServiceStack
{
    public delegate object ObjectActivator(params object[] args);

    public delegate object MethodInvoker(object instance, params object[] args);
    
    public delegate object StaticMethodInvoker(params object[] args);
    
    public delegate void ActionInvoker(object instance, params object[] args);
    
    public delegate void StaticActionInvoker(params object[] args);

    /// <summary>
    /// Delegate to return a different value from an instance (e.g. member accessor)
    /// </summary>
    public delegate object InstanceMapper(object instance);


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

            foreach (var iface in type.GetInterfaces())
            {
                if (iface.IsGenericType && !iface.IsGenericTypeDefinition)
                {
                    foreach (var arg in iface.GetGenericArguments())
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
            var lambda = Expression.Lambda(typeof(ObjectActivator), 
                Expression.Convert(newExp, typeof(object)), 
                paramArgs);

            var ctorFn = (ObjectActivator)lambda.Compile();
            return ctorFn;
        }

        static Dictionary<ConstructorInfo, ObjectActivator> activatorCache = new();

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

        private static Expression[] CreateInvokerParamExpressions(MethodInfo method, ParameterExpression paramArgs)
        {
            var convertFromMethod = typeof(TypeExtensions).GetStaticMethod(nameof(ConvertFromObject));

            var pi = method.GetParameters();
            var exprArgs = new Expression[pi.Length];
            for (int i = 0; i < pi.Length; i++)
            {
                var index = Expression.Constant(i);
                var paramType = pi[i].ParameterType;
                var paramAccessorExp = Expression.ArrayIndex(paramArgs, index);
                var convertParam = convertFromMethod.MakeGenericMethod(paramType);
                exprArgs[i] = Expression.Call(convertParam, paramAccessorExp);
            }

            return exprArgs;
        }

        private static string UseCorrectInvokerErrorMessage(MethodInfo method)
        {
            var invokerName = method.ReturnType == typeof(void) 
                ? (method.IsStatic ? nameof(GetStaticActionInvoker) : nameof(GetActionInvoker)) 
                : (method.IsStatic ? nameof(GetStaticInvoker) : nameof(GetInvoker));
            var invokerType = method.ReturnType == typeof(void) 
                ? (method.IsStatic ? nameof(StaticMethodInvoker) : nameof(MethodInvoker)) 
                : (method.IsStatic ? nameof(StaticActionInvoker) : nameof(ActionInvoker));
            var methodType = method.ReturnType == typeof(void)
                ? (method.IsStatic ? "static void methods" : "instance void methods")
                : (method.IsStatic ? "static methods" : "instance methods"); 
            return $"Use {invokerName} to create a {invokerType} for invoking {methodType}";
        }

        public static MethodInvoker GetInvokerToCache(MethodInfo method)
        {
            if (method.IsStatic)
                throw new NotSupportedException(UseCorrectInvokerErrorMessage(method));
            
            var paramInstance = Expression.Parameter(typeof(object), "instance");
            var paramArgs = Expression.Parameter(typeof(object[]), "args");

            var exprArgs = CreateInvokerParamExpressions(method, paramArgs);
            
            var methodCall = method.DeclaringType.IsValueType 
                ? Expression.Call(Expression.Convert(paramInstance, method.DeclaringType), method, exprArgs)
                : Expression.Call(Expression.TypeAs(paramInstance, method.DeclaringType), method, exprArgs);

            var convertToMethod = typeof(TypeExtensions).GetStaticMethod(nameof(ConvertToObject));
            var convertReturn = convertToMethod.MakeGenericMethod(method.ReturnType);
            
            var lambda = Expression.Lambda(typeof(MethodInvoker), 
                Expression.Call(convertReturn, methodCall), 
                paramInstance, 
                paramArgs);

            var fn = (MethodInvoker)lambda.Compile();
            return fn;
        }

        public static StaticMethodInvoker GetStaticInvokerToCache(MethodInfo method)
        {
            if (!method.IsStatic || method.ReturnType == typeof(void))
                throw new NotSupportedException(UseCorrectInvokerErrorMessage(method));
            
            var paramArgs = Expression.Parameter(typeof(object[]), "args");

            var exprArgs = CreateInvokerParamExpressions(method, paramArgs);

            var methodCall = Expression.Call(method, exprArgs);

            var convertToMethod = typeof(TypeExtensions).GetStaticMethod(nameof(ConvertToObject));
            var convertReturn = convertToMethod.MakeGenericMethod(method.ReturnType);
            
            var lambda = Expression.Lambda(typeof(StaticMethodInvoker), 
                Expression.Call(convertReturn, methodCall), 
                paramArgs);

            var fn = (StaticMethodInvoker)lambda.Compile();
            return fn;
        }

        public static ActionInvoker GetActionInvokerToCache(MethodInfo method)
        {
            if (method.IsStatic || method.ReturnType != typeof(void))
                throw new NotSupportedException(UseCorrectInvokerErrorMessage(method));

            var paramInstance = Expression.Parameter(typeof(object), "instance");
            var paramArgs = Expression.Parameter(typeof(object[]), "args");

            var exprArgs = CreateInvokerParamExpressions(method, paramArgs);
            
            var methodCall = method.DeclaringType.IsValueType 
                ? Expression.Call(Expression.Convert(paramInstance, method.DeclaringType), method, exprArgs)
                : Expression.Call(Expression.TypeAs(paramInstance, method.DeclaringType), method, exprArgs);

            var lambda = Expression.Lambda(typeof(ActionInvoker), 
                methodCall, 
                paramInstance, 
                paramArgs);

            var fn = (ActionInvoker)lambda.Compile();
            return fn;
        }

        public static StaticActionInvoker GetStaticActionInvokerToCache(MethodInfo method)
        {
            if (!method.IsStatic || method.ReturnType != typeof(void))
                throw new NotSupportedException(UseCorrectInvokerErrorMessage(method));

            var paramArgs = Expression.Parameter(typeof(object[]), "args");

            var exprArgs = CreateInvokerParamExpressions(method, paramArgs);
            
            var methodCall = Expression.Call(method, exprArgs);

            var lambda = Expression.Lambda(typeof(StaticActionInvoker), 
                methodCall, 
                paramArgs);

            var fn = (StaticActionInvoker)lambda.Compile();
            return fn;
        }
        
        static Dictionary<MethodInfo, MethodInvoker> invokerCache = new();

        /// <summary>
        /// Create the correct Invoker Delegate Type based on the type of Method
        /// </summary>
        public static Delegate GetInvokerDelegate(this MethodInfo method)
        {
            if (!method.IsStatic)
            {
                if (method.ReturnType != typeof(void))
                    return method.GetInvoker();
                
                return method.GetActionInvoker();
            }

            if (method.ReturnType != typeof(void))
                return method.GetStaticInvoker();
            
            return method.GetStaticActionInvoker();
        }

        /// <summary>
        /// Create an Invoker for public instance methods
        /// </summary>
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
        
        static Dictionary<MethodInfo, StaticMethodInvoker> staticInvokerCache = new();

        /// <summary>
        /// Create an Invoker for public static methods
        /// </summary>
        public static StaticMethodInvoker GetStaticInvoker(this MethodInfo method)
        {
            if (staticInvokerCache.TryGetValue(method, out var fn))
                return fn;

            fn = GetStaticInvokerToCache(method);

            Dictionary<MethodInfo, StaticMethodInvoker> snapshot, newCache;
            do
            {
                snapshot = staticInvokerCache;
                newCache = new Dictionary<MethodInfo, StaticMethodInvoker>(staticInvokerCache) { [method] = fn };

            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref staticInvokerCache, newCache, snapshot), snapshot));

            return fn;
        }
        
        static Dictionary<MethodInfo, ActionInvoker> actionInvokerCache = new();

        /// <summary>
        /// Create an Invoker for public instance void methods
        /// </summary>
        public static ActionInvoker GetActionInvoker(this MethodInfo method)
        {
            if (actionInvokerCache.TryGetValue(method, out var fn))
                return fn;

            fn = GetActionInvokerToCache(method);

            Dictionary<MethodInfo, ActionInvoker> snapshot, newCache;
            do
            {
                snapshot = actionInvokerCache;
                newCache = new Dictionary<MethodInfo, ActionInvoker>(actionInvokerCache) { [method] = fn };

            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref actionInvokerCache, newCache, snapshot), snapshot));

            return fn;
        }
        
        static Dictionary<MethodInfo, StaticActionInvoker> staticActionInvokerCache = new();

        /// <summary>
        /// Create an Invoker for public static void methods
        /// </summary>
        public static StaticActionInvoker GetStaticActionInvoker(this MethodInfo method)
        {
            if (staticActionInvokerCache.TryGetValue(method, out var fn))
                return fn;

            fn = GetStaticActionInvokerToCache(method);

            Dictionary<MethodInfo, StaticActionInvoker> snapshot, newCache;
            do
            {
                snapshot = staticActionInvokerCache;
                newCache = new Dictionary<MethodInfo, StaticActionInvoker>(staticActionInvokerCache) { [method] = fn };

            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref staticActionInvokerCache, newCache, snapshot), snapshot));

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

        public static Func<object,object> GetPropertyAccessor(this Type type, PropertyInfo forProperty)
        {
            var lambda = CreatePropertyAccessorExpression(type, forProperty);
            var fn = (Func<object,object>)lambda.Compile();
            return fn;
        }

        public static LambdaExpression CreatePropertyAccessorExpression(Type type, PropertyInfo forProperty)
        {
            var paramInstance = Expression.Parameter(typeof(object), "instance");

            var castToType = type.IsValueType
                ? Expression.Convert(paramInstance, type)
                : Expression.TypeAs(paramInstance, type);

            var propExpr = Expression.Property(castToType, forProperty);

            var lambda = Expression.Lambda(typeof(Func<object, object>),
                propExpr,
                paramInstance);
            return lambda;
        }
    }

}