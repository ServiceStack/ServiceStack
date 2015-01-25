using System.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

#if SL5
using ServiceStack.Text;
#else
using System.Collections.Concurrent;
using ServiceStack.Text;

#endif

namespace ServiceStack
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

        public static string ToRelativeUri(this IReturn requestDto, string httpMethod, string formatFallbackToPredefinedRoute = null)
        {
            return requestDto.ToUrl(httpMethod, formatFallbackToPredefinedRoute);
        }

        public static string ToRelativeUri(this object requestDto, string httpMethod, string formatFallbackToPredefinedRoute = null)
        {
            return requestDto.ToUrl(httpMethod, formatFallbackToPredefinedRoute);
        }

        /// <summary>
        /// Generate a url from a Request DTO. Pretty URL generation require Routes to be defined using `[Route]` on the Request DTO
        /// </summary>
        public static string ToUrl(this IReturn requestDto, string httpMethod, string formatFallbackToPredefinedRoute = null)
        {
            return ToUrl((object)requestDto, httpMethod, formatFallbackToPredefinedRoute);
        }

        public static string ToGetUrl(this object requestDto)
        {
            return requestDto.ToUrl(HttpMethods.Get, formatFallbackToPredefinedRoute:"json");
        }

        public static string ToPostUrl(this object requestDto)
        {
            return requestDto.ToUrl(HttpMethods.Post, formatFallbackToPredefinedRoute: "json");
        }

        public static string ToPutUrl(this object requestDto)
        {
            return requestDto.ToUrl(HttpMethods.Put, formatFallbackToPredefinedRoute: "json");
        }

        public static string ToDeleteUrl(this object requestDto)
        {
            return requestDto.ToUrl(HttpMethods.Delete, formatFallbackToPredefinedRoute:"json");
        }

        public static string ToOneWayUrlOnly(this object requestDto, string format = "json")
        {
            var requestType = requestDto.GetType();
            return "/{0}/oneway/{1}".Fmt(format, requestType.GetOperationName());
        }

        public static string ToOneWayUrl(this object requestDto, string format = "json")
        {
            var requestType = requestDto.GetType();
            var predefinedRoute = "/{0}/oneway/{1}".Fmt(format, requestType.GetOperationName());
            var queryProperties = RestRoute.GetQueryProperties(requestDto.GetType());
            var queryString = RestRoute.GetQueryString(requestDto, queryProperties);
            if (!string.IsNullOrEmpty(queryString))
                predefinedRoute += "?" + queryString;

            return predefinedRoute;
        }

        public static string ToReplyUrlOnly(this object requestDto, string format = "json")
        {
            var requestType = requestDto.GetType();
            return "/{0}/reply/{1}".Fmt(format, requestType.GetOperationName());
        }

        public static string ToReplyUrl(this object requestDto, string format = "json")
        {
            var requestType = requestDto.GetType();
            var predefinedRoute = "/{0}/reply/{1}".Fmt(format, requestType.GetOperationName());
            var queryProperties = RestRoute.GetQueryProperties(requestDto.GetType());
            var queryString = RestRoute.GetQueryString(requestDto, queryProperties);
            if (!string.IsNullOrEmpty(queryString))
                predefinedRoute += "?" + queryString;

            return predefinedRoute;
        }

        public static string GetOperationName(this Type type)
        {
            var typeName = type.FullName != null //can be null, e.g. generic types
                ? type.FullName.SplitOnFirst("[[").First() //Generic Fullname
                    .Replace(type.Namespace + ".", "") //Trim Namespaces
                    .Replace("+", ".") //Convert nested into normal type
                : type.Name;

            return type.IsGenericParameter ? "'" + typeName : typeName;
        }

        public static string ExpandTypeName(this Type type)
        {
            var typeName = type.IsGenericType()
                ?  ExpandGenericTypeName(type) 
                : type.Name;

            typeName = typeName.Replace('+', '.');

            return type.IsGenericParameter ? "'" + typeName : typeName;
        }

        public static string ExpandGenericTypeName(Type type)
        {
            var nameOnly = type.Name.SplitOnFirst('`')[0];

            var sb = new StringBuilder();
            foreach (var arg in type.GetGenericArguments())
            {
                if (sb.Length > 0)
                    sb.Append(",");

                sb.Append(arg.ExpandTypeName());
            }

            var fullName = "{0}<{1}>".Fmt(nameOnly, sb);
            return fullName;
        }

        public static string ToUrl(this object requestDto, string httpMethod = "GET", string formatFallbackToPredefinedRoute = null)
        {
            httpMethod = httpMethod.ToUpper();

            var requestType = requestDto.GetType();
            var requestRoutes = routesCache.GetOrAdd(requestType, GetRoutesForType);
            if (requestRoutes.Count == 0)
            {
                if (formatFallbackToPredefinedRoute == null)
                    throw new InvalidOperationException("There are no rest routes mapped for '{0}' type. ".Fmt(requestType)
                        + "(Note: The automatic route selection only works with [Route] attributes on the request DTO and "
                        + "not with routes registered in the IAppHost!)");

                var predefinedRoute = "/{0}/reply/{1}".Fmt(formatFallbackToPredefinedRoute, requestType.GetOperationName());
                if (httpMethod == "GET" || httpMethod == "DELETE" || httpMethod == "OPTIONS" || httpMethod == "HEAD")
                {
                    var queryProperties = RestRoute.GetQueryProperties(requestDto.GetType());
                    predefinedRoute += "?" + RestRoute.GetQueryString(requestDto, queryProperties);
                }

                return predefinedRoute;
            }

            var routesApplied = requestRoutes.Select(route => route.Apply(requestDto, httpMethod)).ToList();
            var matchingRoutes = routesApplied.Where(x => x.Matches).ToList();
            if (matchingRoutes.Count == 0)
            {
                var errors = string.Join(String.Empty, routesApplied.Select(x => "\r\n\t{0}:\t{1}".Fmt(x.Route.Path, x.FailReason)).ToArray());
                var errMsg = "None of the given rest routes matches '{0}' request:{1}"
                    .Fmt(requestType.GetOperationName(), errors);

                throw new InvalidOperationException(errMsg);
            }

            RouteResolutionResult matchingRoute;
            if (matchingRoutes.Count > 1)
            {
                matchingRoute = FindMostSpecificRoute(matchingRoutes);
                if (matchingRoute == null)
                {
                    var errors = String.Join(String.Empty, matchingRoutes.Select(x => "\r\n\t" + x.Route.Path).ToArray());
                    var errMsg = "Ambiguous matching routes found for '{0}' request:{1}".Fmt(requestType.Name, errors);
                    throw new InvalidOperationException(errMsg);
                }
            }
            else
            {
                matchingRoute = matchingRoutes[0];
            }

            var url = matchingRoute.Uri;
            if (!httpMethod.HasRequestBody())
            {
                var queryParams = matchingRoute.Route.FormatQueryParameters(requestDto);
                if (!String.IsNullOrEmpty(queryParams))
                {
                    url += "?" + queryParams;
                }
            }

            return url;
        }

        public static bool HasRequestBody(this string httpMethod)
        {
            switch (httpMethod)
            {
                case HttpMethods.Get:
                case HttpMethods.Delete:
                case HttpMethods.Head:
                case HttpMethods.Options:
                    return false;
            }

            return true;
        }

        private static List<RestRoute> GetRoutesForType(Type requestType)
        {
            var restRoutes = requestType.AllAttributes<RouteAttribute>()
                .Select(attr => new RestRoute(requestType, attr.Path, attr.Verbs, attr.Priority))
                .ToList();

            return restRoutes;
        }

        private static RouteResolutionResult FindMostSpecificRoute(IEnumerable<RouteResolutionResult> routes)
        {
            RouteResolutionResult bestMatch = default(RouteResolutionResult);
            var otherMatches = new List<RouteResolutionResult>();

            foreach (var route in routes)
            {
                if (bestMatch == null)
                {
                    bestMatch = route;
                    continue;
                }

                if (route.VariableCount > bestMatch.VariableCount)
                {
                    otherMatches.Clear();
                    bestMatch = route;
                    continue;
                }

                if (route.Priority < bestMatch.Priority)
                {
                    continue;
                }

                if (route.VariableCount == bestMatch.VariableCount)
                {
                    // Choose
                    //     /product-lines/{productId}/{lineNumber}
                    // over
                    //     /products/{productId}/product-lines/{lineNumber}
                    // (shortest one)
                    if (route.PathLength < bestMatch.PathLength)
                    {
                        otherMatches.Add(bestMatch);
                        bestMatch = route;
                    }
                    else
                    {
                        otherMatches.Add(route);
                    }
                }
            }

            // We may find several different routes {code}/{id} and {code}/{name} having the same number of variables. 
            // Such case will be handled by the next check.
            return bestMatch == null || otherMatches.All(r => r.HasSameVariables(bestMatch))
                ? bestMatch
                : null;
        }

        public static string AsHttps(this string absoluteUrl)
        {
            return string.IsNullOrEmpty(absoluteUrl) ? null : absoluteUrl.ReplaceFirst("http://", "https://");
        }

        public static Dictionary<string, Type> GetQueryPropertyTypes(this Type requestType)
        {
            var map = RestRoute.GetQueryProperties(requestType);
            var to = new Dictionary<string, Type>();
            foreach (var entry in map)
            {
                to[entry.Key] = entry.Value.GetMemberType();
            }
            return to;
        }
    }

    public class RestRoute
    {
        private static readonly char[] ArrayBrackets = new[] { '[', ']' };

        private static string FormatValue(object value)
        {
            if (value == null) return null;

            var jsv = value.ToJsv().Trim(ArrayBrackets);
            return jsv;
        }

        public static Func<object, string> FormatVariable = value =>
        {
            if (value == null) return null;

            var valueString = value as string;
            valueString = valueString ?? FormatValue(value);
            return Uri.EscapeDataString(valueString);
        };

        public static Func<object, string> FormatQueryParameterValue = value =>
        {
            if (value == null) return null;

            // Perhaps custom formatting needed for DateTimes, lists, etc.
            var valueString = value as string;
            valueString = valueString ?? FormatValue(value);
            return Uri.EscapeDataString(valueString);
        };

        private const char PathSeparatorChar = '/';
        private const string VariablePrefix = "{";
        private const char VariablePrefixChar = '{';
        private const string VariablePostfix = "}";
        private const char VariablePostfixChar = '}';

        private readonly IDictionary<string, RouteMember> queryProperties = new Dictionary<string, RouteMember>();
        private readonly IDictionary<string, RouteMember> variablesMap = new Dictionary<string, RouteMember>(PclExport.Instance.InvariantComparerIgnoreCase);

        public RestRoute(Type type, string path, string verbs, int priority)
        {
            this.HttpMethods = (verbs ?? string.Empty).Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            this.Type = type;
            this.Path = path;
            this.Priority = priority;

            this.queryProperties = GetQueryProperties(type);
            foreach (var variableName in GetUrlVariables(path))
            {
                var safeVarName = variableName.TrimEnd('*');

                RouteMember propertyInfo;
                if (!this.queryProperties.TryGetValue(safeVarName, out propertyInfo))
                {
                    this.AppendError("Variable '{0}' does not match any property.".Fmt(variableName));
                    continue;
                }

                this.queryProperties.Remove(safeVarName);
                this.variablesMap[variableName] = propertyInfo;
            }
        }

        public string ErrorMsg { get; private set; }

        public Type Type { get; private set; }

        public bool IsValid
        {
            get { return string.IsNullOrEmpty(this.ErrorMsg); }
        }

        public string Path { get; private set; }

        public int Priority { get; private set; }

        public string[] HttpMethods { get; private set; }

        public ICollection<string> Variables
        {
            get { return this.variablesMap.Keys; }
        }

        public List<string> QueryStringVariables
        {
            get { return this.queryProperties.Where(x => !x.Value.IgnoreInQueryString).Select(x => x.Key).ToList(); }
        }

        public RouteResolutionResult Apply(object request, string httpMethod)
        {
            if (!this.IsValid)
            {
                return RouteResolutionResult.Error(this, this.ErrorMsg);
            }

            if (HttpMethods != null && HttpMethods.Length != 0 && httpMethod != null && !HttpMethods.Contains(httpMethod) && !HttpMethods.Contains("ANY"))
            {
                return RouteResolutionResult.Error(this, "Allowed HTTP methods '{0}' does not support the specified '{1}' method."
                    .Fmt(HttpMethods.Join(", "), httpMethod));
            }

            var uri = this.Path;

            var unmatchedVariables = new List<string>();
            foreach (var variable in this.variablesMap)
            {
                var property = variable.Value;
                var value = property.GetValue(request);
                var isWildCard = variable.Key.EndsWith("*");
                if (value == null && !isWildCard)
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
                return RouteResolutionResult.Error(this, errMsg);
            }

            return RouteResolutionResult.Success(this, uri);
        }

        public string FormatQueryParameters(object request)
        {
            return GetQueryString(request, this.queryProperties);
        }

        internal static string GetQueryString(object request, IDictionary<string, RouteMember> propertyMap)
        {
            var result = new StringBuilder();

            foreach (var queryProperty in propertyMap)
            {
                if (queryProperty.Value.IgnoreInQueryString)
                    continue;

                var value = queryProperty.Value.GetValue(request, true);
                if (value == null)
                    continue;

                var qsName = ServiceStack.Text.JsConfig.EmitLowercaseUnderscoreNames
                    ? queryProperty.Key.ToLowercaseUnderscore()
                    : queryProperty.Key;

                result.Append(qsName)
                    .Append('=')
                    .Append(FormatQueryParameterValue(value))
                    .Append('&');
            }

            if (result.Length > 0) result.Length -= 1;
            return result.ToString();
        }

        internal static IDictionary<string, RouteMember> GetQueryProperties(Type requestType)
        {
            var result = new Dictionary<string, RouteMember>(PclExport.Instance.InvariantComparerIgnoreCase);
            var hasDataContract = requestType.HasAttribute<DataContractAttribute>();

            foreach (var propertyInfo in requestType.GetPublicProperties())
            {
                var propertyName = propertyInfo.Name;

                if (!propertyInfo.CanRead) continue;
                if (hasDataContract)
                {
                    if (!propertyInfo.HasAttribute<DataMemberAttribute>()) continue;

                    var dataMember = propertyInfo.FirstAttribute<DataMemberAttribute>();
                    if (!string.IsNullOrEmpty(dataMember.Name))
                    {
                        propertyName = dataMember.Name;
                    }
                }

                result[propertyName.ToCamelCase()] = new PropertyRouteMember(propertyInfo)
                {
                    IgnoreInQueryString = propertyInfo.FirstAttribute<IgnoreDataMemberAttribute>() != null, //but allow in PathInfo
                };
            }

            if (JsConfig.IncludePublicFields)
            {
                foreach (var fieldInfo in requestType.GetPublicFields())
                {
                    var fieldName = fieldInfo.Name;

                    result[fieldName.ToCamelCase()] = new FieldRouteMember(fieldInfo)
                    {
                        IgnoreInQueryString = fieldInfo.FirstAttribute<IgnoreDataMemberAttribute>() != null, //but allow in PathInfo
                    };
                }

            }

            return result;
        }

        private IEnumerable<string> GetUrlVariables(string path)
        {
            var components = path.Split(PathSeparatorChar);
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

                    yield return variableName;
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
    }

    public class RouteResolutionResult
    {
        public string FailReason { get; private set; }
        public string Uri { get; private set; }
        public RestRoute Route { get; private set; }

        public bool Matches
        {
            get { return string.IsNullOrEmpty(this.FailReason); }
        }

        public static RouteResolutionResult Error(RestRoute route, string errorMsg)
        {
            return new RouteResolutionResult { Route = route, FailReason = errorMsg };
        }

        public static RouteResolutionResult Success(RestRoute route, string uri)
        {
            return new RouteResolutionResult { Route = route, Uri = uri, Priority = route.Priority };
        }

        internal int Priority { get; set; }

        internal int VariableCount
        {
            get { return Route.Variables.Count; }
        }

        internal int PathLength
        {
            get { return Route.Path.Length; }
        }

        internal bool HasSameVariables(RouteResolutionResult other)
        {
            return Route.Variables.All(v => other.Route.Variables.Contains(v));
        }
    }


}