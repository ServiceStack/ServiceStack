using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ServiceStack.Common.Expressions
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
			ParameterExpression instanceParameter = Expression.Parameter(typeof(object), "target");
			ParameterExpression argumentsParameter = Expression.Parameter(typeof(object[]), "arguments");

			MethodCallExpression call = Expression.Call(
				Expression.Convert(instanceParameter, method.DeclaringType),
				method,
				CreateParameterExpressions(method, argumentsParameter));

			Expression<LateBoundMethod> lambda = Expression.Lambda<LateBoundMethod>(
				Expression.Convert(call, typeof(object)),
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
			ParameterExpression instanceParameter = Expression.Parameter(typeof(object), "target");
			ParameterExpression argumentsParameter = Expression.Parameter(typeof(object[]), "arguments");

			MethodCallExpression call = Expression.Call(
				Expression.Convert(instanceParameter, method.DeclaringType),
				method,
				CreateParameterExpressions(method, argumentsParameter));

			var lambda = Expression.Lambda<LateBoundVoid>(
				Expression.Convert(call, method.ReturnParameter.ParameterType),
				instanceParameter,
				argumentsParameter);

			return lambda.Compile();
		}


	}


	public class TestExample2
	{

		public void HideTheExpressionTreeGutsInAnotherMethodBelow()
		{
			/*
			var order = new Order { OrderID = "FUN!" };

			GetPropValueAndExecuteMethod(
				order,
				o => o.OrderID,
				x => Console.WriteLine(x)
			);
			 * */
		}

		public void GetPropValueAndExecuteMethod<A>(
			A target,
			Expression<Func<A, object>> propertyExpression,
			Expression<Action<object>> methodExpression)
		{

			// Define the parameters for the various lambda expression
			// i.e. the 'x' in (x => x.Foo)
			var parameters = propertyExpression.Parameters;

			// Create an 'Invoke' expression to actually execute the property P getter
			var getPropValueExpression = Expression.Invoke(
												propertyExpression,
												parameters.Cast<Expression>());

			// Create a 'Call' expression to call the function with the value of property P
			var methodBody = methodExpression.Body as MethodCallExpression;
			var methodCall = Expression.Call(methodBody.Method, getPropValueExpression);

			// Wrap it in a lambda so we can actually call it at runtime
			var finalCall = Expression.Lambda<Action<A>>(methodCall, parameters);

			// Compile the lambda to get an actual Action<A>
			var action = finalCall.Compile();

			// Execute it!
			action(target);
		}


	}
}