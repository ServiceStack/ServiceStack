using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ServiceStack.WebHost.EndPoints.Support.Markdown
{
	public static class DataBinder
	{
		public static Func<object, string> CompileToString(Type type, string expr)
		{
			var param = Expression.Parameter(typeof(object), "model");
			Expression body = Expression.Convert(param, type);

			var members = expr.Split('.');
			for (int i = 0; i < members.Length; i++)
			{
				body = Expression.PropertyOrField(body, members[i]);
			}

			var method = typeof(Convert).GetMethod("ToString",
				BindingFlags.Static | BindingFlags.Public,
				null, new[] { body.Type }, null);

			if (method == null)
			{
				method = typeof(Convert).GetMethod("ToString",
					BindingFlags.Static | BindingFlags.Public,
					null, new[] { typeof(object) }, null);

				body = Expression.Call(method, Expression.Convert(body, typeof(object)));
			}
			else
			{
				body = Expression.Call(method, body);
			}

			return Expression.Lambda<Func<object, string>>(body, param).Compile();
		}

		public static Func<object, object> Compile(Type type, string expr)
		{
			var param = Expression.Parameter(typeof(object), "model");
			Expression body = Expression.Convert(param, type);

			var members = expr.Split('.');
			for (var i = 1; i < members.Length; i++)
			{
				body = Expression.PropertyOrField(body, members[i]);
			}

			body = Expression.Convert(body, typeof (object));

			return Expression.Lambda<Func<object, object>>(body, param).Compile();
		}

		public static Func<TModel, TProp> Compile<TModel, TProp>(string expression)
		{
			var propNames = expression.Split('.');

			var model = Expression.Parameter(typeof(TModel), "model");

			Expression body = model;
			foreach (string propName in propNames.Skip(1))
				body = Expression.Property(body, propName);
			//Debug.WriteLine(prop);

			if (body.Type != typeof(TProp))
				body = Expression.Convert(body, typeof(TProp));

			Func<TModel, TProp> func = Expression.Lambda<Func<TModel, TProp>>(body, model).Compile();
			//TODO: cache funcs
			return func;
		}
	}
}