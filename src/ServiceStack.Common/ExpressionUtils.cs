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
    }
}