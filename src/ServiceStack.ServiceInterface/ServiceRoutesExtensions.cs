using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ServiceStack.ServiceHost;

namespace ServiceStack.ServiceInterface
{
	public static class ServiceRoutesExtensions
	{
		/// <summary>
		///     Scans the supplied Assemblies to infer REST paths and HTTP verbs.
		/// </summary>
		///<param name="routes">The <see cref="IServiceRoutes"/> instance.</param>
		///<param name="assembliesWithServices">
		///     The assemblies with REST services.
		/// </param>
		/// <returns>The same <see cref="IServiceRoutes"/> instance;
		///		never <see langword="null"/>.</returns>
		public static IServiceRoutes AddFromAssembly(this IServiceRoutes routes,
													 params Assembly[] assembliesWithServices)
		{
			foreach (Assembly assembly in assembliesWithServices)
			{
				IEnumerable<Type> restServices = 
                    from t in assembly.GetExportedTypes()
					where
						!t.IsAbstract &&
						t.IsSubclassOfRawGeneric(typeof(RestServiceBase<>))
					select t;

				foreach (Type restService in restServices)
				{
					Type baseType = restService.BaseType;

					//go up the hierarchy to the first generic base type
					while (!baseType.IsGenericType)
					{
						baseType = baseType.BaseType;
					}

					Type requestType = baseType.GetGenericArguments()[0];

					//find overriden REST methods
					string allowedMethods = "";
					if (restService.GetMethod("OnGet").DeclaringType == restService)
					{
						allowedMethods += "GET ";
					}

					if (restService.GetMethod("OnPost").DeclaringType == restService)
					{
						allowedMethods += "POST ";
					}

					if (restService.GetMethod("OnPut").DeclaringType == restService)
					{
						allowedMethods += "PUT ";
					}

					if (restService.GetMethod("OnDelete").DeclaringType == restService)
					{
						allowedMethods += "DELETE ";
					}

					if (restService.GetMethod("OnPatch").DeclaringType == restService)
					{
						allowedMethods += "PATCH ";
					}

					routes.Add(requestType, restService.Name, allowedMethods, null);
				}
			}

			return routes;
		}

		public static bool IsSubclassOfRawGeneric(this Type toCheck, Type generic)
		{
			while (toCheck != typeof(object))
			{
				Type cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
				if (generic == cur)
				{
					return true;
				}
				toCheck = toCheck.BaseType;
			}
			return false;
		}

        private static string FormatRoute<T>(string path, params Expression<Func<T, object>>[] propertyExpressions)
        {
            var properties = propertyExpressions.Select(x => string.Format("{{{0}}}", PropertyName(x))).ToArray();
            return string.Format(path, properties);
        }

        private static string PropertyName(LambdaExpression ex)
        {
            return (ex.Body is UnaryExpression ? (MemberExpression)((UnaryExpression)ex.Body).Operand : (MemberExpression)ex.Body).Member.Name;
        }

        public static void Add<T>(this IServiceRoutes serviceRoutes, string httpMethod, string url, params Expression<Func<T, object>>[] propertyExpressions)
        {
            serviceRoutes.Add<T>(FormatRoute(url, propertyExpressions), httpMethod);
        }
	}
}