using System;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceInterface
{
    public class RouteFor<T>
    {
        readonly string path;
        string verbs;

        public static string Path
        {
            get
            {
                var routeAttribute = TypeDescriptor.GetAttributes(typeof(T)).OfType<RouteAttribute>().SingleOrDefault();
                ThrowRouteNotDefined(routeAttribute);
                return routeAttribute.Path;
            }
        }

        public static string Verbs
        {
            get
            {
                var routeAttribute = TypeDescriptor.GetAttributes(typeof(T)).OfType<RouteAttribute>().Single();
                ThrowRouteNotDefined(routeAttribute);
                return routeAttribute.Verbs;
            }
        }

        static void ThrowRouteNotDefined(RouteAttribute routeAttribute)
        {
            if (routeAttribute == null)
                throw new NullReferenceException(string.Format("Route not defined for {0}", typeof(T).Name));
        }

        RouteFor(string path)
        {
            this.path = path;
            AddOrUpdateAttribute();
        }

        public static RouteFor<T> WithPath(string path)
        {
            return new RouteFor<T>(path);
        }

        public static RouteFor<T> WithPath(string path, params Expression<Func<T, object>>[] expressions)
        {
            return WithPath(FormatRoute(path, expressions));
        }

        static string FormatRoute(string path, params Expression<Func<T, object>>[] propertyExpressions)
        {
            path = Regex.Replace(path, "({\\D*})", "{${1}}");
            var properties = propertyExpressions.Select(x => string.Format("{{{0}}}", PropertyName(x))).ToArray();
            return string.Format(path, properties);
        }

        static string PropertyName(LambdaExpression lambdaExpression)
        {
            return (lambdaExpression.Body is UnaryExpression ? (MemberExpression)((UnaryExpression)lambdaExpression.Body).Operand : (MemberExpression)lambdaExpression.Body).Member.Name;
        }

        public RouteFor<T> AndVerbs(params string[] verbs)
        {
            this.verbs = string.Join(",", verbs);
            AddOrUpdateAttribute();
            return this;
        }

        void AddOrUpdateAttribute()
        {

            var attribute = TypeDescriptor.GetAttributes(typeof (T)).OfType<RouteAttribute>().SingleOrDefault();
            if (attribute == null)
                TypeDescriptor.AddAttributes(typeof(T), new RouteAttribute(path, verbs));
            else
            {
                attribute.Path = path;
                attribute.Verbs = verbs;
            }
        }

        public void Create()
        {
            EndpointHostConfig.Instance.ServiceManager.ServiceController.RegisterRestPaths(typeof(T));
        }
    }
}