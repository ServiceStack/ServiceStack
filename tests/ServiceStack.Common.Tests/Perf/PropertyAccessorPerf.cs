using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.Reflection;
using ServiceStack.Text;

namespace ServiceStack.Common.Tests.Perf
{
    [Ignore("Benchmark for comparing property access")]
    [TestFixture]
    public class PropertyAccessorPerf
        : PerfTestBase
    {
        public PropertyAccessorPerf()
        {
            this.MultipleIterations = new List<int> { 1000000 };
        }

        public static class TestAcessor<TEntity>
        {
            public static Func<TEntity, TId> TypedGetPropertyFn<TId>(PropertyInfo pi)
            {
                var mi = pi.GetGetMethod();
#if NETCORE
                return (Func<TEntity, TId>)mi.CreateDelegate(typeof(Func<TEntity, TId>));
#else
                return (Func<TEntity, TId>)Delegate.CreateDelegate(typeof(Func<TEntity, TId>), mi);
#endif
            }

            /// <summary>
            /// Required to cast the return ValueType to an object for caching
            /// </summary>
            public static Func<TEntity, object> ValueUnTypedGetPropertyFn<TId>(PropertyInfo pi)
            {
                var typedPropertyFn = TypedGetPropertyFn<TId>(pi);
                return x => typedPropertyFn(x);
            }

            public static Func<TEntity, object> ValueUnTypedGetPropertyTypeFn_Reflection(PropertyInfo pi)
            {
                var mi = typeof(StaticAccessors<TEntity>).GetMethod("TypedGetPropertyFn");
                var genericMi = mi.MakeGenericMethod(pi.PropertyType);
                var typedGetPropertyFn = (Delegate)genericMi.Invoke(null, new[] { pi });
#if NETCORE
                return x => typedGetPropertyFn.GetMethodInfo().Invoke(x, new object[] { });
#else
                return x => typedGetPropertyFn.Method.Invoke(x, new object[] { });
#endif
            }

            public static Func<TEntity, object> ValueUnTypedGetPropertyTypeFn_Expr(PropertyInfo pi)
            {
                var mi = typeof(StaticAccessors<TEntity>).GetMethod("TypedGetPropertyFn");
                var genericMi = mi.MakeGenericMethod(pi.PropertyType);
                var typedGetPropertyFn = (Delegate)genericMi.Invoke(null, new[] { pi });

#if NETCORE
                var typedMi = typedGetPropertyFn.GetMethodInfo();
#else
                var typedMi = typedGetPropertyFn.Method;
#endif
                var obj = Expression.Parameter(typeof(object), "oFunc");
                var expr = Expression.Lambda<Func<TEntity, object>>(
                        Expression.Convert(
                            Expression.Call(
                                Expression.Convert(obj, typedMi.DeclaringType),
                                typedMi
                            ),
                            typeof(object)
                        ),
                        obj
                    );
                return expr.Compile();
            }


            /// <summary>
            /// Func to set the Strongly-typed field
            /// </summary>
            public static Action<TEntity, TId> TypedSetPropertyFn<TId>(PropertyInfo pi)
            {
                var mi = pi.GetSetMethod();
#if NETCORE
                return (Action<TEntity, TId>)mi.CreateDelegate(typeof(Action<TEntity, TId>));
#else
                return (Action<TEntity, TId>)Delegate.CreateDelegate(typeof(Action<TEntity, TId>), mi);
#endif
            }

            /// <summary>
            /// Required to cast the ValueType to an object for caching
            /// </summary>
            public static Action<TEntity, object> ValueUnTypedSetPropertyFn<TId>(PropertyInfo pi)
            {
                var typedPropertyFn = TypedSetPropertyFn<TId>(pi);
                return (x, y) => typedPropertyFn(x, (TId)y);
            }

            public static Action<TEntity, object> ValueUnTypedSetPropertyTypeFn_Reflection(PropertyInfo pi)
            {
                var mi = typeof(StaticAccessors<TEntity>).GetMethod("TypedSetPropertyFn");
                var genericMi = mi.MakeGenericMethod(pi.PropertyType);
                var typedSetPropertyFn = (Delegate)genericMi.Invoke(null, new[] { pi });

#if NETCORE
                return (x, y) => typedSetPropertyFn.GetMethodInfo().Invoke(x, new[] { y });
#else
                return (x, y) => typedSetPropertyFn.Method.Invoke(x, new[] { y });
#endif
            }

            public static Action<TEntity, object> ValueUnTypedSetPropertyTypeFn_Expr(PropertyInfo pi)
            {
                var mi = typeof(StaticAccessors<TEntity>).GetMethod("TypedSetPropertyFn");
                var genericMi = mi.MakeGenericMethod(pi.PropertyType);
                var typedSetPropertyFn = (Delegate)genericMi.Invoke(null, new[] { pi });

#if NETCORE
                var typedMi = typedSetPropertyFn.GetMethodInfo();
#else
                var typedMi = typedSetPropertyFn.Method;
#endif
                var paramFunc = Expression.Parameter(typeof(object), "oFunc");
                var paramValue = Expression.Parameter(typeof(object), "oValue");
                var expr = Expression.Lambda<Action<TEntity, object>>(
                        Expression.Call(
                            Expression.Convert(paramFunc, typedMi.DeclaringType),
                            typedMi,
                            Expression.Convert(paramValue, pi.PropertyType)
                        ),
                        paramFunc,
                        paramValue
                    );
                return expr.Compile();
            }
        }

        private void CompareGet<T>(Func<T, object> reflection, Func<T, object> expr)
        {
            var obj = typeof(T).CreateInstance<T>();
            CompareMultipleRuns(
                "GET Reflection", () => reflection(obj),
                "GET Expression", () => expr(obj)
            );
        }

        private void CompareSet<T, TArg>(
            Action<T, object> reflection, Action<T, object> expr, TArg arg)
        {
            var obj = typeof(T).CreateInstance<T>();
            CompareMultipleRuns(
                "SET Reflection", () => reflection(obj, arg),
                "SET Expression", () => expr(obj, arg)
            );
        }

        [Test]
        public void Compare_get_int()
        {
            var fieldPi = typeof(ModelWithIdAndName).GetProperty("Id");
            CompareGet
                (
                    TestAcessor<ModelWithIdAndName>.ValueUnTypedGetPropertyTypeFn_Reflection(fieldPi),
                    TestAcessor<ModelWithIdAndName>.ValueUnTypedGetPropertyTypeFn_Expr(fieldPi)
                );
            CompareSet
                (
                    TestAcessor<ModelWithIdAndName>.ValueUnTypedSetPropertyTypeFn_Reflection(fieldPi),
                    TestAcessor<ModelWithIdAndName>.ValueUnTypedSetPropertyTypeFn_Expr(fieldPi),
                    1
                );
        }

        [Test]
        public void Compare_get_string()
        {
            var fieldPi = typeof(ModelWithIdAndName).GetProperty("Name");
            CompareGet
                (
                    TestAcessor<ModelWithIdAndName>.ValueUnTypedGetPropertyTypeFn_Reflection(fieldPi),
                    TestAcessor<ModelWithIdAndName>.ValueUnTypedGetPropertyTypeFn_Expr(fieldPi)
                );

            CompareSet
                (
                    TestAcessor<ModelWithIdAndName>.ValueUnTypedSetPropertyTypeFn_Reflection(fieldPi),
                    TestAcessor<ModelWithIdAndName>.ValueUnTypedSetPropertyTypeFn_Expr(fieldPi),
                    "A"
                );
        }
    }
}