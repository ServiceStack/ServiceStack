using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ServiceStack.Logging;

namespace ServiceStack
{
    public static class ExpressionUtils
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ExpressionUtils));

        public static PropertyInfo ToPropertyInfo(this Expression fieldExpr)
        {
            return ToPropertyInfo(fieldExpr as LambdaExpression)
                ?? ToPropertyInfo(fieldExpr as MemberExpression);
        }

        public static PropertyInfo ToPropertyInfo(LambdaExpression lambda)
        {
            return lambda?.Body.NodeType == ExpressionType.MemberAccess 
                ? ToPropertyInfo(lambda.Body as MemberExpression) 
                : null;
        }

        public static PropertyInfo ToPropertyInfo(MemberExpression m)
        {
            var pi = m?.Member as PropertyInfo;
            return pi;
        }

        public static string GetMemberName<T>(Expression<Func<T, object>> fieldExpr)
        {
            var m = GetMemberExpression(fieldExpr);
            if (m != null)
                return m.Member.Name;

            throw new NotSupportedException("Expected Property Expression");
        }

        public static MemberExpression GetMemberExpression<T>(Expression<Func<T, object>> expr)
        {
            var member = expr.Body as MemberExpression;
            var unary = expr.Body as UnaryExpression;
            return member ?? unary?.Operand as MemberExpression;
        }

        public static Dictionary<string, object> AssignedValues<T>(this Expression<Func<T>> expr)
        {
            if (!(expr?.Body is MemberInitExpression initExpr))
                return null;

            var to = new Dictionary<string, object>();
            foreach (MemberBinding binding in initExpr.Bindings)
            {
                to[binding.Member.Name] = binding.GetValue();
            }
            return to;
        }

        public static string[] GetFieldNames<T>(this Expression<Func<T, object>> expr)
        {
            if (expr.Body is MemberExpression member)
            {
                if (member.Member.DeclaringType.IsAssignableFrom(typeof(T)))
                    return new[] { member.Member.Name };

                var array = CachedExpressionCompiler.Evaluate(member);
                if (array is IEnumerable<string> strEnum)
                    return strEnum.ToArray();
            }

            if (expr.Body is NewExpression newExpr)
                return newExpr.Arguments.OfType<MemberExpression>().Select(x => x.Member.Name).ToArray();

            if (expr.Body is MemberInitExpression init)
                return init.Bindings.Select(x => x.Member.Name).ToArray();

            if (expr.Body is NewArrayExpression newArray)
            {
                var constantExprs = newArray.Expressions.OfType<ConstantExpression>().ToList();
                if (newArray.Expressions.Count == constantExprs.Count)
                    return constantExprs.Select(x => x.Value.ToString()).ToArray();

                var array = CachedExpressionCompiler.Evaluate(newArray);
                if (array is string[] strArray)
                    return strArray;

                return array.ConvertTo<string[]>();
            }

            if (expr.Body is UnaryExpression unary)
            {
                member = unary.Operand as MemberExpression;
                if (member != null)
                    return new[] { member.Member.Name };
            }

            throw new ArgumentException("Invalid Fields List Expression: " + expr);
        }

        public static object GetValue(this MemberBinding binding)
        {
            switch (binding.BindingType)
            {
                case MemberBindingType.Assignment:
                    var assign = (MemberAssignment)binding;
                    if (assign.Expression is ConstantExpression constant)
                        return constant.Value;

                    try
                    {
                        return CachedExpressionCompiler.Evaluate(assign.Expression);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error compiling expression in MemberBinding.GetValue()", ex);

                        //Fallback to compile and execute
                        var member = Expression.Convert(assign.Expression, typeof(object));
                        var lambda = Expression.Lambda<Func<object>>(member);
                        var getter = lambda.Compile();
                        return getter();
                    }
            }
            return null;
        }
    }
}