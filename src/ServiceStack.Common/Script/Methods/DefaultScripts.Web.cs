using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using ServiceStack.Script;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Script
{
    // ReSharper disable InconsistentNaming
    
    public partial class DefaultScripts
    {
        internal IHttpRequest req(ScriptScopeContext scope) => scope.GetValue("Request") as IHttpRequest;

        public bool matchesPathInfo(ScriptScopeContext scope, string pathInfo) => 
            scope.GetValue("PathInfo")?.ToString().TrimEnd('/') == pathInfo?.TrimEnd('/');

        public bool startsWithPathInfo(ScriptScopeContext scope, string pathInfo) => pathInfo == "/"
            ? matchesPathInfo(scope, pathInfo)
            : scope.GetValue("PathInfo")?.ToString().TrimEnd('/').StartsWith(pathInfo?.TrimEnd('/') ?? "") == true;

        public object ifMatchesPathInfo(ScriptScopeContext scope, object returnTarget, string pathInfo) =>
            matchesPathInfo(scope, pathInfo) ? returnTarget : null;

        public bool isHttpGet(ScriptScopeContext scope) => req(scope)?.Verb == HttpMethods.Get;
        public bool isHttpPost(ScriptScopeContext scope) => req(scope)?.Verb == HttpMethods.Post;
        public bool isHttpPut(ScriptScopeContext scope) => req(scope)?.Verb == HttpMethods.Put;
        public bool isHttpDelete(ScriptScopeContext scope) => req(scope)?.Verb == HttpMethods.Delete;
        public bool isHttpPatch(ScriptScopeContext scope) => req(scope)?.Verb == HttpMethods.Patch;

        public object ifHttpGet(ScriptScopeContext scope, object ignoreTarget) => ifHttpGet(scope);
        public object ifHttpGet(ScriptScopeContext scope) => isHttpGet(scope) ? (object)IgnoreResult.Value : StopExecution.Value;
        public object ifHttpPost(ScriptScopeContext scope, object ignoreTarget) => ifHttpPost(scope);
        public object ifHttpPost(ScriptScopeContext scope) => isHttpPost(scope) ? (object)IgnoreResult.Value : StopExecution.Value;
        public object ifHttpPut(ScriptScopeContext scope, object ignoreTarget) => ifHttpPut(scope);
        public object ifHttpPut(ScriptScopeContext scope) => isHttpPut(scope) ? (object)IgnoreResult.Value : StopExecution.Value;
        public object ifHttpDelete(ScriptScopeContext scope, object ignoreTarget) => ifHttpDelete(scope);
        public object ifHttpDelete(ScriptScopeContext scope) => isHttpDelete(scope) ? (object)IgnoreResult.Value : StopExecution.Value;
        public object ifHttpPatch(ScriptScopeContext scope, object ignoreTarget) => ifHttpPatch(scope);
        public object ifHttpPatch(ScriptScopeContext scope) => isHttpPatch(scope) ? (object)IgnoreResult.Value : StopExecution.Value;

        public object importRequestParams(ScriptScopeContext scope)
        {
            var args = req(scope).GetRequestParams();
            foreach (var entry in args)
            {
                scope.ScopedParams[entry.Key] = entry.Value;
            }
            return StopExecution.Value;
        }

        public object importRequestParams(ScriptScopeContext scope, object onlyImportArgNames)
        {
            var args = req(scope).GetRequestParams();
            var names = toVarNames(onlyImportArgNames);
            foreach (var name in names)
            {
                if (args.TryGetValue(name, out var value))
                    scope.ScopedParams[name] = value;

            }
            return StopExecution.Value;
        }
        
        public NameValueCollection form(ScriptScopeContext scope) => req(scope).FormData;
        public NameValueCollection query(ScriptScopeContext scope) => req(scope).QueryString;
        public NameValueCollection qs(ScriptScopeContext scope) => req(scope).QueryString;
        public string queryString(ScriptScopeContext scope)
        {
            var qs = req(scope).QueryString.ToString();
            return string.IsNullOrEmpty(qs) ? qs : "?" + qs;
        }

        public Dictionary<string, object> queryDictionary(ScriptScopeContext scope) =>
            req(scope).QueryString.ToObjectDictionary();
        
        public Dictionary<string, object> formDictionary(ScriptScopeContext scope) =>
            req(scope).FormData.ToObjectDictionary();
        
        public string toQueryString(object keyValuePairs)
        {
            var sb = StringBuilderCache.Allocate();
            var i = 0;
            if (keyValuePairs is IEnumerable<KeyValuePair<string, object>> kvps)
            {
                foreach (var entry in kvps)
                {
                    if (i++ > 0)
                        sb.Append('&');

                    sb.Append(entry.Key + "=" + entry.Value?.ToString().UrlEncode());
                }
            }
            else if (keyValuePairs is IDictionary d)
            {
                foreach (var key in d.Keys)
                {
                    if (i++ > 0)
                        sb.Append('&');

                    sb.Append(key + "=" + d[key]?.ToString().UrlEncode());
                }
            }
            else if (keyValuePairs is NameValueCollection nvc)
            {
                foreach (string key in nvc)
                {
                    if (key == null)
                        continue;
                    if (i++ > 0)
                        sb.Append('&');
                    sb.Append(key + "=" + nvc[key].UrlEncode());
                }
            }
            else throw new NotSupportedException($"{nameof(toQueryString)} expects a collection of KeyValuePair's but was '{keyValuePairs.GetType().Name}'");
            
            return StringBuilderCache.ReturnAndFree(sb.Length > 0 ? sb.Insert(0,'?') : sb);
        }

        public string httpMethod(ScriptScopeContext scope) => req(scope)?.Verb;
        public string httpRequestUrl(ScriptScopeContext scope) => req(scope)?.AbsoluteUri;
        public string httpPathInfo(ScriptScopeContext scope) => scope.GetValue("PathInfo")?.ToString();

        public string formQuery(ScriptScopeContext scope, string name) => ViewUtils.FormQuery(req(scope), name);

        public string[] formQueryValues(ScriptScopeContext scope, string name) => ViewUtils.FormQueryValues(req(scope), name);
        public string httpParam(ScriptScopeContext scope, string name) => ViewUtils.GetParam(req(scope), name);

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
            catch (ArgumentException)
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