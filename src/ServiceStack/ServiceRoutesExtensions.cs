using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ServiceStack.Host;
using ServiceStack.Web;

namespace ServiceStack
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
                AddNewApiRoutes(routes, assembly);
            }

            return routes;
        }

        private static void AddNewApiRoutes(IServiceRoutes routes, Assembly assembly)
        {
            var services = assembly.GetExportedTypes()
                .Where(t => !t.IsAbstract()
                            && t.HasInterface(typeof(IService)));

            foreach (Type service in services)
            {
                var allServiceActions = service.GetActions();
                foreach (var requestDtoActions in allServiceActions.GroupBy(x => x.GetParameters()[0].ParameterType))
                {
                    var requestType = requestDtoActions.Key;
                    var hasWildcard = requestDtoActions.Any(x => x.Name.EqualsIgnoreCase(ActionContext.AnyAction));
                    string allowedVerbs = null; //null == All Routes
                    if (!hasWildcard)
                    {
                        var allowedMethods = new List<string>();
                        foreach (var action in requestDtoActions)
                        {
                            allowedMethods.Add(action.Name.ToUpper());
                        }

                        if (allowedMethods.Count == 0) continue;
                        allowedVerbs = string.Join(" ", allowedMethods.ToArray());
                    }

                    routes.AddRoute(requestType, allowedVerbs);
                }
            }
        }

        private static void AddRoute(this IServiceRoutes routes, Type requestType, string allowedVerbs)
        {
            foreach (var strategy in HostContext.Config.RouteNamingConventions)
            {
                strategy(routes, requestType, allowedVerbs);
            }
        }

        public static IServiceRoutes Add<TRequest>(this IServiceRoutes routes, string restPath, ApplyTo verbs)
        {
            return routes.Add<TRequest>(restPath, verbs.ToVerbsString());
        }

        public static IServiceRoutes Add(this IServiceRoutes routes, Type requestType, string restPath, ApplyTo verbs)
        {
            return routes.Add(requestType, restPath, verbs.ToVerbsString());
        }

        private static string ToVerbsString(this ApplyTo verbs)
        {
            var allowedMethods = new List<string>();
            foreach (var entry in ApplyToUtils.ApplyToVerbs)
            {
                if (verbs.Has(entry.Key))
                    allowedMethods.Add(entry.Value);
            }

            return string.Join(" ", allowedMethods.ToArray());
        }

        public static bool IsSubclassOfRawGeneric(this Type toCheck, Type generic)
        {
            while (toCheck != typeof(object))
            {
                Type cur = toCheck.IsGenericType() ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur)
                {
                    return true;
                }
                toCheck = toCheck.BaseType();
            }
            return false;
        }

        private static string FormatRoute<T>(string restPath, params Expression<Func<T, object>>[] propertyExpressions)
        {
            var properties = propertyExpressions.Select(x => $"{{{PropertyName(x)}}}").ToArray();
            return string.Format(restPath, properties);
        }

        private static string PropertyName(LambdaExpression lambdaExpression)
        {
            return (lambdaExpression.Body is UnaryExpression ? (MemberExpression)((UnaryExpression)lambdaExpression.Body).Operand : (MemberExpression)lambdaExpression.Body).Member.Name;
        }

        public static IServiceRoutes Add<T>(this IServiceRoutes serviceRoutes, string restPath, ApplyTo verbs, params Expression<Func<T, object>>[] propertyExpressions)
        {
            return serviceRoutes.Add<T>(FormatRoute(restPath, propertyExpressions), verbs);
        }
    }
}