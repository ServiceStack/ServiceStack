using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ServiceStack.Reflection
{
    public static class DelegateFactory
    {
        /*
         *	MethodInfo method = typeof(String).GetMethod("StartsWith", new[] { typeof(string) });  
            LateBoundMethod callback = DelegateFactory.Create(method);  
  
            string foo = "this is a test";  
            bool result = (bool) callback(foo, new[] { "this" });  
  
            result.ShouldBeTrue();  
         */
        public delegate object LateBoundMethod(object target, object[] arguments);

        public static LateBoundMethod Create(MethodInfo method)
        {
            if (method == null)
                throw new System.ArgumentNullException(nameof(method));

            ParameterExpression instanceParameter = Expression.Parameter(typeof(object), "target");
            ParameterExpression argumentsParameter = Expression.Parameter(typeof(object[]), "arguments");

            Expression instance = method.IsStatic
                ? null
                : Expression.Convert(instanceParameter, method.DeclaringType);

            MethodCallExpression call = Expression.Call(
                instance,
                method,
                CreateParameterExpressions(method, argumentsParameter));

            Expression body = method.ReturnType == typeof(void)
                ? Expression.Block(call, Expression.Constant(null, typeof(object)))
                : Expression.Convert(call, typeof(object));

            Expression<LateBoundMethod> lambda = Expression.Lambda<LateBoundMethod>(
                body,
                instanceParameter,
                argumentsParameter);

            return lambda.Compile();
        }

        private static Expression[] CreateParameterExpressions(MethodInfo method, Expression argumentsParameter)
        {
            return method.GetParameters().Select((parameter, index) =>
                Expression.Convert(
                    Expression.ArrayIndex(argumentsParameter, Expression.Constant(index)),
                    parameter.ParameterType)).ToArray();
        }


        public delegate void LateBoundVoid(object target, object[] arguments);

        public static LateBoundVoid CreateVoid(MethodInfo method)
        {
            if (method == null)
                throw new System.ArgumentNullException(nameof(method));

            ParameterExpression instanceParameter = Expression.Parameter(typeof(object), "target");
            ParameterExpression argumentsParameter = Expression.Parameter(typeof(object[]), "arguments");

            Expression instance = method.IsStatic
                ? null
                : Expression.Convert(instanceParameter, method.DeclaringType);

            MethodCallExpression call = Expression.Call(
                instance,
                method,
                CreateParameterExpressions(method, argumentsParameter));

            Expression body = method.ReturnType == typeof(void)
                ? (Expression)call
                : Expression.Block(call, Expression.Empty());

            var lambda = Expression.Lambda<LateBoundVoid>(
                body,
                instanceParameter,
                argumentsParameter);

            return lambda.Compile();
        }
    }
}