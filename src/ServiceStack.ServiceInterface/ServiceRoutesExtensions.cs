using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ServiceStack.Common;
using ServiceStack.Common.Utils;
using ServiceStack.Common.Web;
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
                IEnumerable<Type> services = 
                    from t in assembly.GetExportedTypes()
                    where
                        !t.IsAbstract &&
                        t.IsSubclassOfRawGeneric(typeof(ServiceBase<>))
                    select t;

                foreach (Type service in services)
                {
                    Type baseType = service.BaseType;
                    //go up the hierarchy to the first generic base type
                    while (!baseType.IsGenericType)
                    {
                        baseType = baseType.BaseType;
                    }

                    Type requestType = baseType.GetGenericArguments()[0];

                    string allowedVerbs = null; //null == All Routes

                    if (service.IsSubclassOfRawGeneric(typeof(RestServiceBase<>)))
                    {
                        //find overriden REST methods
                        var allowedMethods = new List<string>();
                        if (service.GetMethod("OnGet").DeclaringType == service)
                        {
                            allowedMethods.Add(HttpMethods.Get);
                        }

                        if (service.GetMethod("OnPost").DeclaringType == service)
                        {
                            allowedMethods.Add(HttpMethods.Post);
                        }

                        if (service.GetMethod("OnPut").DeclaringType == service)
                        {
                            allowedMethods.Add(HttpMethods.Put);
                        }

                        if (service.GetMethod("OnDelete").DeclaringType == service)
                        {
                            allowedMethods.Add(HttpMethods.Delete);
                        }

                        if (service.GetMethod("OnPatch").DeclaringType == service)
                        {
                            allowedMethods.Add(HttpMethods.Patch);
                        }

                        if (allowedMethods.Count == 0) continue;
                        allowedVerbs = string.Join(" ", allowedMethods.ToArray());
                    }

                    routes.Add(requestType, requestType.Name, allowedVerbs);

                    var hasIdField = requestType.GetProperty(IdUtils.IdField) != null;
                    if (hasIdField)
                    {
                        var routePath = requestType.Name + "/{" + IdUtils.IdField + "}";
                        routes.Add(requestType, routePath, allowedVerbs);
                    }
                }
            }

            return routes;
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
                Type cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur)
                {
                    return true;
                }
                toCheck = toCheck.BaseType;
            }
            return false;
        }

        private static string FormatRoute<T>(string restPath, params Expression<Func<T, object>>[] propertyExpressions)
        {
            var properties = propertyExpressions.Select(x => string.Format("{{{0}}}", PropertyName(x))).ToArray();
            return string.Format(restPath, properties);
        }

        private static string PropertyName(LambdaExpression lambdaExpression)
        {
            return (lambdaExpression.Body is UnaryExpression ? (MemberExpression)((UnaryExpression)lambdaExpression.Body).Operand : (MemberExpression)lambdaExpression.Body).Member.Name;
        }

        public static void Add<T>(this IServiceRoutes serviceRoutes, string restPath, ApplyTo verbs, params Expression<Func<T, object>>[] propertyExpressions)
        {
            serviceRoutes.Add<T>(FormatRoute(restPath, propertyExpressions), verbs);
        }
    }
}