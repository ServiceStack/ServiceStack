using System;
using System.Collections;
using System.Collections.Generic;
using ServiceStack.Web;

namespace ServiceStack.Templates
{
    // ReSharper disable InconsistentNaming
    
    public partial class TemplateDefaultFilters
    {
        private IHttpRequest req(TemplateScopeContext scope) => scope.GetValue("Request") as IHttpRequest;

        public bool matchesPathInfo(TemplateScopeContext scope, string pathInfo) => 
            scope.GetValue("PathInfo")?.ToString().TrimEnd('/') == pathInfo?.TrimEnd('/');

        public bool startsWithPathInfo(TemplateScopeContext scope, string pathInfo) => pathInfo == "/"
            ? matchesPathInfo(scope, pathInfo)
            : scope.GetValue("PathInfo")?.ToString().TrimEnd('/').StartsWith(pathInfo?.TrimEnd('/') ?? "") == true;

        public object ifMatchesPathInfo(TemplateScopeContext scope, object returnTarget, string pathInfo) =>
            matchesPathInfo(scope, pathInfo) ? returnTarget : null;

        public bool isHttpGet(TemplateScopeContext scope) => req(scope)?.Verb == HttpMethods.Get;
        public bool isHttpPost(TemplateScopeContext scope) => req(scope)?.Verb == HttpMethods.Post;
        public bool isHttpPut(TemplateScopeContext scope) => req(scope)?.Verb == HttpMethods.Put;
        public bool isHttpDelete(TemplateScopeContext scope) => req(scope)?.Verb == HttpMethods.Delete;
        public bool isHttpPatch(TemplateScopeContext scope) => req(scope)?.Verb == HttpMethods.Patch;

        [HandleUnknownValue] public object ifHttpGet(TemplateScopeContext scope, object ignoreTarget) => ifHttpGet(scope);
        [HandleUnknownValue] public object ifHttpGet(TemplateScopeContext scope) => isHttpGet(scope) ? (object)IgnoreResult.Value : StopExecution.Value;
        [HandleUnknownValue] public object ifHttpPost(TemplateScopeContext scope, object ignoreTarget) => ifHttpPost(scope);
        [HandleUnknownValue] public object ifHttpPost(TemplateScopeContext scope) => isHttpPost(scope) ? (object)IgnoreResult.Value : StopExecution.Value;
        [HandleUnknownValue] public object ifHttpPut(TemplateScopeContext scope, object ignoreTarget) => ifHttpPut(scope);
        [HandleUnknownValue] public object ifHttpPut(TemplateScopeContext scope) => isHttpPut(scope) ? (object)IgnoreResult.Value : StopExecution.Value;
        [HandleUnknownValue] public object ifHttpDelete(TemplateScopeContext scope, object ignoreTarget) => ifHttpDelete(scope);
        [HandleUnknownValue] public object ifHttpDelete(TemplateScopeContext scope) => isHttpDelete(scope) ? (object)IgnoreResult.Value : StopExecution.Value;
        [HandleUnknownValue] public object ifHttpPatch(TemplateScopeContext scope, object ignoreTarget) => ifHttpPatch(scope);
        [HandleUnknownValue] public object ifHttpPatch(TemplateScopeContext scope) => isHttpPatch(scope) ? (object)IgnoreResult.Value : StopExecution.Value;
 
        public string httpMethod(TemplateScopeContext scope) => req(scope)?.Verb;
        public string httpRequestUrl(TemplateScopeContext scope) => req(scope)?.AbsoluteUri;
        public string httpPathInfo(TemplateScopeContext scope) => scope.GetValue("PathInfo")?.ToString();

        public string httpForm(TemplateScopeContext scope, string name)
        {
            var httpReq = req(scope);
            return httpReq.FormData[name] ?? httpReq.QueryString[name];
        }

        public string[] httpFormValues(TemplateScopeContext scope, string name)
        {
            var httpReq = req(scope);
            var values = httpReq.Verb == HttpMethods.Post 
                ? httpReq.FormData.GetValues(name) 
                : httpReq.QueryString.GetValues(name);

            return values?.Length == 1 // if it's only a single item can be returned in comma-delimited list
                ? values[0].Split(',') 
                : values ?? TypeConstants.EmptyStringArray;
        }

        public string httpFormData(TemplateScopeContext scope, string name) => req(scope).FormData[name];
        
        public string httpQueryString(TemplateScopeContext scope, string name) => req(scope).QueryString[name];
        public string httpParam(TemplateScopeContext scope, string name) => GetParam(req(scope), name);

        private static string GetParam(IRequest httpReq, string name) //sync with IRequest.GetParam()
        {
            string value;
            if ((value = httpReq.Headers[HttpHeaders.XParamOverridePrefix + name]) != null) return value;
            if ((value = httpReq.QueryString[name]) != null) return value;
            if ((value = httpReq.FormData[name]) != null) return value;

            //IIS will assign null to params without a name: .../?some_value can be retrieved as req.Params[null]
            //TryGetValue is not happy with null dictionary keys, so we should bail out here
            if (string.IsNullOrEmpty(name)) return null;

            if (httpReq.Cookies.TryGetValue(name, out var cookie)) return cookie.Value;

            if (httpReq.Items.TryGetValue(name, out var oValue)) return oValue.ToString();

            return null;
        }

        public string urlEncode(string value, bool upperCase) => value.UrlEncode(upperCase);
        public string urlEncode(string value) => value.UrlEncode();
        public string urlDecode(string value) => value.UrlDecode();

        public string htmlEncode(string value) => value.HtmlEncode();
        public string htmlDecode(string value) => value.HtmlDecode();
        
        public bool containsXss(object target)
        {
            try
            {
                return MatchesStringValue(target, ContainsXss);
            }
            catch (ArgumentException ex)
            {
                throw new NotSupportedException($"containsXss cannot validate {target.GetType().Name}");
            }
        }

        public static bool MatchesStringValue(object target, Func<string, bool> match)
        {
            if (target == null)
                return false;

            if (target is string str)
            {
                if (match(str))
                    return true;
            }
            else if (target is IDictionary d)
            {
                foreach (var item in d.Values)
                {
                    if (item is string s && match(s))
                        return true;
                }
            }
            else if (target is IEnumerable objs)
            {
                foreach (var item in objs)
                {
                    if (item is string s && match(s))
                        return true;
                }
            }
            else throw new ArgumentException(nameof(target));
            
            return false;
        }
        
        // tests for https://www.owasp.org/index.php/OWASP_Testing_Guide_Appendix_C:_Fuzz_Vectors#Cross_Site_Scripting_.28XSS.29
        public static string[] XssFragments = { //greedy list
            "<script",
            "javascript:",
            "%3A",       //= ':' URL Encode
            "&#0000058", //= ':' HTML Entity Encode
            "SRC=#",
            "SRC=/",
            "SRC=&",
            "SRC= ",
            "onload=",
            "onload =",
            "onunload=",
            "onerror=",
            "@import",
            ":url(",
        };

        public static bool ContainsXss(string text)
        {
            foreach (var needle in XssFragments)
            {
                var pos = text.IndexOf(needle, 0, StringComparison.OrdinalIgnoreCase);
                if (pos >= 0)
                    return true;
            }
            return false;
        }
    }
}