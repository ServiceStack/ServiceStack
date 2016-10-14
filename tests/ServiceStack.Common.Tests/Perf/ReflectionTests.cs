using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;

namespace ServiceStack.Common.Tests.Perf
{
    [Ignore("Benchmark for comparing expressions / delegates around generic methods.")]
    [TestFixture]
    public class ReflectionTests
        : PerfTestBase
    {
        public ReflectionTests()
            : base()
        {
            this.MultipleIterations = new List<int> { 100000000 };
        }

        public static Func<object, object> GetPropertyValueMethodViaExpressions(
            Type type, PropertyInfo propertyInfo)
        {
            var getMethodInfo = propertyInfo.GetGetMethod();
            var oInstanceParam = Expression.Parameter(typeof(object), "oInstanceParam");
            var instanceParam = Expression.Convert(oInstanceParam, type);

            var exprCallPropertyGetFn = Expression.Call(instanceParam, getMethodInfo);
            var oExprCallPropertyGetFn = Expression.Convert(exprCallPropertyGetFn, typeof(object));

            var propertyGetFn = Expression.Lambda<Func<object, object>>
            (
                oExprCallPropertyGetFn,
                oInstanceParam
            ).Compile();

            return propertyGetFn;
        }

        public static Func<object, object> GetPropertyValueMethodViaDelegate(
            Type type, PropertyInfo propertyInfo)
        {
            var mi = typeof(ReflectionTests).GetMethod("CreateFunc");

            var genericMi = mi.MakeGenericMethod(type, propertyInfo.PropertyType);
            var del = genericMi.Invoke(null, new[] { propertyInfo.GetGetMethod() });

            return (Func<object, object>)del;
        }

        public static Func<object, object> CreateFunc<T1, T2>(MethodInfo mi)
        {
#if !NETCORE
            var del = (Func<T1, T2>)Delegate.CreateDelegate(typeof(Func<T1, T2>), mi);
#else
            var del = (Func<T1, T2>)mi.CreateDelegate(typeof(Func<T1, T2>));
#endif
            return x => del((T1)x);
        }

        [Test]
        public void Compare()
        {
            var model = ModelWithIdAndName.Create(1);
            var pi = model.GetType().GetProperty("Name");
            var simpleExpr = GetPropertyValueMethodViaExpressions(typeof(ModelWithIdAndName), pi);
            var simpleDelegate = GetPropertyValueMethodViaDelegate(typeof(ModelWithIdAndName), pi);

            CompareMultipleRuns(
                "Expressions", () => simpleExpr(model),
                "Delegate", () => simpleDelegate(model)
            );

        }

    }
}