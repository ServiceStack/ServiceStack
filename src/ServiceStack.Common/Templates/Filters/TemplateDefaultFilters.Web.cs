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
    }
}