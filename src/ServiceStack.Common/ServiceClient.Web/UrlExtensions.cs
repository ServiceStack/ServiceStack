using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using ServiceStack.Net30.Collections.Concurrent;
using ServiceStack.ServiceHost;
using ServiceStack.Text;

namespace ServiceStack.ServiceClient.Web
{
    /// <summary>
    /// Donated by Ivan Korneliuk from his post:
    /// http://korneliuk.blogspot.com/2012/08/servicestack-reusing-dtos.html
    /// 
    /// Modified to only allow using routes matching the supplied HTTP Verb
    /// </summary>
    public static class UrlExtensions
    {
        private static readonly ConcurrentDictionary<Type, List<RestRoute>> routesCache =
            new ConcurrentDictionary<Type, List<RestRoute>>();

        public static string ToUrl(this IReturn request, string httpMethod, string formatFallbackToPredefinedRoute=null)
        {
            httpMethod = httpMethod.ToUpper();

            var requestType = request.GetType();
            var requestRoutes = routesCache.GetOrAdd(requestType, GetRoutesForType);
            if (!requestRoutes.Any())
            {
                if (formatFallbackToPredefinedRoute == null)
                    throw new InvalidOperationException("There is no rest routes mapped for '{0}' type. ".Fmt(requestType)
                        + "(Note: The automatic route selection only works with [Route] attributes on the request DTO and" 
                        + "not with routes registered in the IAppHost!)");

                var predefinedRoute = "/{0}/syncreply/{1}".Fmt(formatFallbackToPredefinedRoute, request.GetType().Name);
                if (httpMethod == "GET" || httpMethod == "DELETE" || httpMethod == "OPTIONS")
                {
                    var queryString = "?{0}".Fmt(request.GetType().GetProperties().ToQueryString(request));
                    predefinedRoute += queryString;
                }

                return predefinedRoute;
            }

            var routesApplied =
                requestRoutes.Select(route => new { Route = route, Result = route.Apply(request, httpMethod) }).ToList();
            var matchingRoutes = routesApplied.Where(x => x.Result.Matches).ToList();
            if (!matchingRoutes.Any())
            {
                var errors = string.Join(String.Empty, routesApplied.Select(x => "\r\n\t{0}:\t{1}".Fmt(x.Route.Path, x.Result.FailReason)).ToArray());
                var errMsg = "None of the given rest routes matches '{0}' request:{1}"
                    .Fmt(requestType.Name, errors);

                throw new InvalidOperationException(errMsg);
            }

            var matchingRoute = matchingRoutes[0]; // hack to determine variable type.
            if (matchingRoutes.Count > 1)
            {
                var mostSpecificRoute = FindMostSpecificRoute(matchingRoutes.Select(x => x.Route));
                if (mostSpecificRoute == null)
                {
                    var errors = String.Join(String.Empty, matchingRoutes.Select(x => "\r\n\t" + x.Route.Path).ToArray());
                    var errMsg = "Ambiguous matching routes found for '{0}' request:{1}".Fmt(requestType.Name, errors);
                    throw new InvalidOperationException(errMsg);
                }

                matchingRoute = matchingRoutes.Single(x => x.Route == mostSpecificRoute);
            }
            else
            {
                matchingRoute = matchingRoutes.Single();
            }
            
            var url = matchingRoute.Result.Uri;
            if (httpMethod == HttpMethod.Get || httpMethod == HttpMethod.Delete)
            {
                var queryParams = matchingRoute.Route.FormatQueryParameters(request);
                if (!String.IsNullOrEmpty(queryParams))
                {
                    url += "?" + queryParams;
                }
            }

            return url;
        }

        private static List<RestRoute> GetRoutesForType(Type requestType)
        {
            var restRoutes = requestType.GetCustomAttributes(false)
                .OfType<RouteAttribute>()
                .Select(attr => new RestRoute(requestType, attr.Path, attr.Verbs))
                .ToList();

            return restRoutes;
        }

        private static RestRoute FindMostSpecificRoute(IEnumerable<RestRoute> routes)
        {
            routes = routes.ToList();
            var mostSpecificRoute = routes.OrderBy(p => p.Variables.Count).Last();

            // We may find several different routes {code}/{id} and {code}/{name} having the same number of variables. 
            // Such case will be handled by the next check.
            var allPathesAreSubsetsOfMostSpecific = routes
                .All(route => !route.Variables.Except(mostSpecificRoute.Variables).Any());
            if (!allPathesAreSubsetsOfMostSpecific)
            {
                return null;
            }

            // Choose
            //     /product-lines/{productId}/{lineNumber}
            // over
            //     /products/{productId}/product-lines/{lineNumber}
            // (shortest one)
            var shortestPath = routes
                .Where(p => p.Variables.Count == mostSpecificRoute.Variables.Count)
                .OrderBy(path => path.Path.Length)
                .First();

            return shortestPath;
        }

        public static string ToQueryString(this IEnumerable<PropertyInfo> propertyInfos, object request)
        {
            var parameters = String.Empty;
            foreach (var property in propertyInfos)
            {
                var value = property.GetValue(request, null);
                if (value == null)
                {
                    continue;
                }
                parameters += "&{0}={1}".Fmt(property.Name.ToCamelCase(), RestRoute.FormatQueryParameterValue(value));
            }
            if (!String.IsNullOrEmpty(parameters))
            {
                parameters = parameters.Substring(1);
            }
            return parameters;
        }
    }

    public class RestRoute
    {
        static char[] ArrayBrackets = new[]{'[',']'};

        public static string FormatValue(object value)
        {
            var jsv = value.ToJsv().Trim(ArrayBrackets);
            return jsv;
        }

        [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Using field is just easier.")]
        public static Func<object, string> FormatVariable = value => {
            var valueString = value as string;
            return valueString != null ? Uri.EscapeDataString(valueString) : FormatValue(value);
        };

        [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Using field is just easier.")]
        public static Func<object, string> FormatQueryParameterValue = value => {
            // Perhaps custom formatting needed for DateTimes, lists, etc.
            var valueString = value as string;
            return valueString != null ? Uri.EscapeDataString(valueString) : FormatValue(value);
        };

        private const char PathSeparatorChar = '/';
        private const string VariablePrefix = "{";
        private const char VariablePrefixChar = '{';
        private const string VariablePostfix = "}";
        private const char VariablePostfixChar = '}';

        private readonly Dictionary<string, PropertyInfo> variablesMap = new Dictionary<string, PropertyInfo>();

        public RestRoute(Type type, string path, string verbs)
        {
            this.HttpMethods = (verbs ?? string.Empty).Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            this.Type = type;
            this.Path = path;

            this.MapUrlVariablesToProperties();
            this.Variables = this.variablesMap.Keys.Select(x => x.ToLowerInvariant()).Distinct().ToList().AsReadOnly();
        }

        public string ErrorMsg { get; set; }

        public Type Type { get; set; }

        public bool IsValid
        {
            get { return string.IsNullOrEmpty(this.ErrorMsg); }
        }

        public string Path { get; set; }

        public string[] HttpMethods { get; private set; }

        public IList<string> Variables { get; set; }

        public RouteResolutionResult Apply(object request, string httpMethod)
        {
            if (!this.IsValid)
            {
                return RouteResolutionResult.Error(this.ErrorMsg);
            }

            if (HttpMethods != null && HttpMethods.Length != 0 && httpMethod != null && !HttpMethods.Contains(httpMethod) && !HttpMethods.Contains("ANY"))
            {
                return RouteResolutionResult.Error("Allowed HTTP methods '{0}' does not support the specified '{1}' method."
                    .Fmt(HttpMethods.Join(", "), httpMethod));
            }

            var uri = this.Path;

            var unmatchedVariables = new List<string>();
            foreach (var variable in this.variablesMap)
            {
                var property = variable.Value;
                var value = property.GetValue(request, null);
                if (value == null)
                {
                    unmatchedVariables.Add(variable.Key);
                    continue;
                }

                var variableValue = FormatVariable(value);
                uri = uri.Replace(VariablePrefix + variable.Key + VariablePostfix, variableValue);
            }

            if (unmatchedVariables.Any())
            {
                var errMsg = "Could not match following variables: " + string.Join(",", unmatchedVariables.ToArray());
                return RouteResolutionResult.Error(errMsg);
            }

            return RouteResolutionResult.Success(uri);
        }

        public string FormatQueryParameters(object request)
        {
            var propertyInfos = this.Type.GetProperties().Except(this.variablesMap.Values);

            var parameters = propertyInfos.ToQueryString(request);

            return parameters;
        }

        private void MapUrlVariablesToProperties()
        {
            // Perhaps other filters needed: do not include indexers, property should have public getter, etc.
            var properties = this.Type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            var components = this.Path.Split(PathSeparatorChar);
            foreach (var component in components)
            {
                if (string.IsNullOrEmpty(component))
                {
                    continue;
                }

                if (component.Contains(VariablePrefix) || component.Contains(VariablePostfix))
                {
                    var variableName = component.Substring(1, component.Length - 2);

                    // Accept only variables matching this format: '/{property}/'
                    // Incorrect formats: '/{property/' or '/{property}-some-other-text/'
                    // I'm not sure that the second one will be parsed correctly at server side.
                    if (component[0] != VariablePrefixChar || component[component.Length - 1] != VariablePostfixChar || variableName.Contains(VariablePostfix))
                    {
                        this.AppendError("Component '{0}' can not be parsed".Fmt(component));
                        continue;
                    }

                    if (!this.variablesMap.ContainsKey(variableName))
                    {
                        var matchingProperties = properties
                            .Where(p => p.Name.Equals(variableName, StringComparison.OrdinalIgnoreCase))
                            .ToList();

                        if (!matchingProperties.Any())
                        {
                            this.AppendError("Variable '{0}' does not match any property.".Fmt(variableName));
                            continue;
                        }

                        if (matchingProperties.Count > 1)
                        {
                            var msg = "Variable '{0}' matches '{1}' properties which are differ by case only."
                                .Fmt(variableName, matchingProperties.Count);
                            this.AppendError(msg);
                            continue;
                        }

                        this.variablesMap.Add(variableName, matchingProperties.Single());
                    }
                }
            }
        }

        private void AppendError(string msg)
        {
            if (string.IsNullOrEmpty(this.ErrorMsg))
            {
                this.ErrorMsg = msg;
            }
            else
            {
                this.ErrorMsg += "\r\n" + msg;
            }
        }

        public class RouteResolutionResult
        {
            public string FailReason { get; private set; }

            public string Uri { get; private set; }

            public bool Matches
            {
                get { return string.IsNullOrEmpty(this.FailReason); }
            }

            public static RouteResolutionResult Error(string errorMsg)
            {
                return new RouteResolutionResult { FailReason = errorMsg };
            }

            public static RouteResolutionResult Success(string uri)
            {
                return new RouteResolutionResult { Uri = uri };
            }
        }
    }
}