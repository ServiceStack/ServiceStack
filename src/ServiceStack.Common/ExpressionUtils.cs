using System;
using System.Linq.Expressions;
using System.Reflection;

namespace ServiceStack
{
    public static class ExpressionUtils
    {
        public static PropertyInfo ToPropertyInfo(this Expression fieldExpr)
        {
            return ToPropertyInfo(fieldExpr as LambdaExpression)
                ?? ToPropertyInfo(fieldExpr as MemberExpression);
        }

        public static PropertyInfo ToPropertyInfo(LambdaExpression lambda)
        {
            if (lambda == null)
                return null;

            return lambda.Body.NodeType == ExpressionType.MemberAccess 
                ? ToPropertyInfo(lambda.Body as MemberExpression) 
                : null;
        }

        public static PropertyInfo ToPropertyInfo(MemberExpression m)
        {
            if (m == null)
                return null;

            var pi = m.Member as PropertyInfo;
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
            return member ?? (unary != null ? unary.Operand as MemberExpression : null);
        }
    }
}