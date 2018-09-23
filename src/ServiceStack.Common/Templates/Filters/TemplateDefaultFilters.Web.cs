using System;
using System.Collections;
using ServiceStack.Web;

namespace ServiceStack.Templates
{
    // ReSharper disable InconsistentNaming
    
    public partial class TemplateDefaultFilters
    {
        private IHttpRequest req(TemplateScopeContext scope) => scope.GetValue("Request") as IHttpRequest;

        public bool matchesPathInfo(TemplateScopeContext scope, string pathInfo) => 
            scope.GetValue("PathInfo")?.ToString().TrimEnd('/') == pathInfo?.TrimEnd('/');

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