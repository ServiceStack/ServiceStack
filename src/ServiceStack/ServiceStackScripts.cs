using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ServiceStack.Auth;
using ServiceStack.Host;
using ServiceStack.Html;
using ServiceStack.IO;
using ServiceStack.Script;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
    [Obsolete("Use ServiceStackScripts")]
    public class TemplateServiceStackFilters : ServiceStackScripts {}
    
    public partial class ServiceStackScripts : ScriptMethods
    {
        private ServiceStackHost appHost => HostContext.AppHost;

        public IHttpRequest getHttpRequest(ScriptScopeContext scope) => req(scope);
        internal IHttpRequest req(ScriptScopeContext scope) => scope.GetValue("Request") as IHttpRequest;

        public object sendToGateway(ScriptScopeContext scope, string requestName) => 
            sendToGateway(scope, TypeConstants.EmptyObjectDictionary, requestName, null);
        public object sendToGateway(ScriptScopeContext scope, object dto, string requestName) => sendToGateway(scope, dto, requestName, null);
        public object sendToGateway(ScriptScopeContext scope, object dto, string requestName, object options)
        {
            try
            {
                if (requestName == null)
                    throw new ArgumentNullException(nameof(requestName));
                if (dto == null)
                    throw new ArgumentNullException(nameof(dto));
                
                var gateway = appHost.GetServiceGateway(req(scope));
                var requestType = appHost.Metadata.GetOperationType(requestName);
                if (requestType == null)
                    throw new ArgumentException("Request DTO not found: " + requestName);

                var responseType = appHost.Metadata.GetResponseTypeByRequest(requestType);

                var requestDto = dto.GetType() == requestType
                    ? dto
                    : dto is Dictionary<string, object> objDictionary
                        ? objDictionary.FromObjectDictionary(requestType)
                        : dto.ConvertTo(requestType);

                var response = gateway.Send(responseType, requestDto);
                return response;
            }
            catch (Exception ex)
            {
                throw new StopFilterExecutionException(scope, options, ex);
            }
        }

        public object publishToGateway(ScriptScopeContext scope, string requestName) => 
            publishToGateway(scope, TypeConstants.EmptyObjectDictionary, requestName, null);
        public object publishToGateway(ScriptScopeContext scope, object dto, string requestName) => publishToGateway(scope, dto, requestName, null);
        public object publishToGateway(ScriptScopeContext scope, object dto, string requestName, object options)
        {
            try
            {
                if (requestName == null)
                    throw new ArgumentNullException(nameof(requestName));
                if (dto == null)
                    throw new ArgumentNullException(nameof(dto));
                
                var gateway = appHost.GetServiceGateway(req(scope));
                var requestType = appHost.Metadata.GetOperationType(requestName);
                if (requestType == null)
                    throw new ArgumentException("Request DTO not found: " + requestName);

                var requestDto = dto.GetType() == requestType
                    ? dto
                    : dto is Dictionary<string, object> objDictionary
                        ? objDictionary.FromObjectDictionary(requestType)
                        : dto.ConvertTo(requestType);

                gateway.Publish(requestDto);
                return StopExecution.Value;
            }
            catch (Exception ex)
            {
                throw new StopFilterExecutionException(scope, options, ex);
            }
        }
        
        public object sendToAutoQuery(ScriptScopeContext scope, string requestName) => 
            sendToAutoQuery(scope, TypeConstants.EmptyObjectDictionary, requestName, null);
        public object sendToAutoQuery(ScriptScopeContext scope, object dto, string requestName) => sendToAutoQuery(scope, dto, requestName, null);
        public object sendToAutoQuery(ScriptScopeContext scope, object dto, string requestName, object options)
        {
            try
            {
                if (requestName == null)
                    throw new ArgumentNullException(nameof(requestName));
                if (dto == null)
                    throw new ArgumentNullException(nameof(dto));

                var requestType = appHost.Metadata.GetOperationType(requestName);
                if (requestType == null)
                    throw new ArgumentException("Request DTO not found: " + requestName);

                if (requestType.HasInterface(typeof(IQueryDb)))
                {
                    var ssFilter = Context.ScriptMethods.FirstOrDefault(x => x is IAutoQueryDbFilters) as IAutoQueryDbFilters;
                    if (ssFilter == null)
                        throw new NotImplementedException("sendToAutoQuery RDBMS requires TemplateAutoQueryFilters");

                    return ssFilter.sendToAutoQuery(scope, dto, requestName, options);
                }
                
                var autoQuery = appHost.TryResolve<IAutoQueryData>();
                if (autoQuery == null)
                    throw new NotSupportedException("The AutoQueryDataFeature plugin is not registered.");

                var objDictionary = dto is Dictionary<string, object> od ? od : null;
                
                var requestDto = objDictionary != null 
                    ? objDictionary.FromObjectDictionary(requestType)
                    : dto.GetType() == requestType
                        ? dto
                        : dto.ConvertTo(requestType);
                
                if (!(requestDto is IQueryData aqDto))
                    throw new ArgumentException("Request DTO is not an AutoQuery Data DTO: " + requestName);
                                
                var reqParams = objDictionary?.ToStringDictionary() ?? TypeConstants.EmptyStringDictionary;
                var q = autoQuery.CreateQuery(aqDto, reqParams, req(scope));
                var response = autoQuery.Execute(aqDto, q);

                return response;
            }
            catch (Exception ex)
            {
                throw new StopFilterExecutionException(scope, options, ex);
            }
        }

        public object toResults(object dto)
        {
            var results = AutoQueryDataServiceSource.GetResults(dto);
            return results;
        }
       
        public object getUserSession(ScriptScopeContext scope) => req(scope).GetSession();
        public IAuthSession userSession(ScriptScopeContext scope) => req(scope).GetSession();

        public bool isAuthenticated(ScriptScopeContext scope)
        {
            var authSession = userSession(scope);
            return authSession?.IsAuthenticated == true;
        }

        public object redirectIfNotAuthenticated(ScriptScopeContext scope, string path)
        {
            if (!isAuthenticated(scope))
            {
                return Context.DefaultScripts.@return(scope, new HttpResult(null, null, HttpStatusCode.Redirect) {
                    Headers = {
                        [HttpHeaders.Location] = path.FirstCharEquals('~')
                            ? req(scope).ResolveAbsoluteUrl(path)
                            : path
                    }
                });                
            }
            
            return IgnoreResult.Value;
        }

        [HandleUnknownValue] public object ifAuthenticated(ScriptScopeContext scope) => isAuthenticated(scope) 
            ? (object)IgnoreResult.Value : StopExecution.Value;
       
        [HandleUnknownValue] public object ifNotAuthenticated(ScriptScopeContext scope) => !isAuthenticated(scope) 
            ? (object)IgnoreResult.Value : StopExecution.Value;
        
        [HandleUnknownValue] public object onlyIfAuthenticated(ScriptScopeContext scope, object value) => isAuthenticated(scope) 
            ? value : StopExecution.Value;

        [HandleUnknownValue] public object endIfAuthenticated(ScriptScopeContext scope, object value) => !isAuthenticated(scope) 
            ? value : StopExecution.Value;

        public IHttpResult getHttpResult(ScriptScopeContext scope, object options) => httpResult(scope, options);
        public HttpResult httpResult(ScriptScopeContext scope, object options)
        {
            var args = scope.AssertOptions(nameof(httpResult), options);
            return ToHttpResult(args);
        }

        public static HttpResult ToHttpResult(Dictionary<string, object> args)
        {
            var statusCode = HttpStatusCode.OK;
            if (args.TryGetValue("status", out var oStatus))
            {
                if (oStatus is int status)
                    statusCode = (HttpStatusCode) status;
                if (oStatus is string strStatus)
                    statusCode = (HttpStatusCode) Enum.Parse(typeof(HttpStatusCode), strStatus);

                args.Remove("status");
            }
            args.TryGetValue("statusDescription", out var statusDescription);

            object response = null;
            if (args.TryGetValue("response", out var oResponse))
            {
                response = oResponse;
                args.Remove("response");
            }

            string contentType = null;
            if (args.TryGetValue("contentType", out var oContentType))
            {
                contentType = (string) oContentType;
                args.Remove("contentType");
            }
            else if (args.TryGetValue("format", out var oFormat) && oFormat is string format)
            {
                contentType = HostContext.ContentTypes.GetFormatContentType(format);
                args.Remove("format");
            }

            var to = new HttpResult(response, contentType, statusCode) {
                StatusDescription = statusDescription as string
            };
            var httpResultHeaders = args.ToStringDictionary();
            httpResultHeaders.Each(x => to.Options[x.Key] = x.Value);
            return to;
        }

        public bool hasErrorStatus(ScriptScopeContext scope) => getErrorStatus(scope) != null;

        public ResponseStatus getErrorStatus(ScriptScopeContext scope) => 
            scope.GetValue("errorStatus") as ResponseStatus ??
            ViewUtils.GetErrorStatus(req(scope));

        /// <summary>
        /// Only return form input value if form submission was invalid
        /// </summary>
        public string formValue(ScriptScopeContext scope, string name) => formValue(scope, name, null);

        public string formValue(ScriptScopeContext scope, string name, string defaultValue) => hasErrorStatus(scope) 
            ? ViewUtils.FormQuery(req(scope), name) 
            : defaultValue;

        public string[] formValues(ScriptScopeContext scope, string name) => hasErrorStatus(scope) 
            ? ViewUtils.FormQueryValues(req(scope), name) 
            : null;
    
        public bool formCheckValue(ScriptScopeContext scope, string name)
        {
            var value = formValue(scope, name);
            return value == "true" || value == "True" || value == "t" || value == "on" || value == "1";
        }
        
        public string errorResponseSummary(ScriptScopeContext scope) => errorResponseSummary(scope, getErrorStatus(scope));

        public string errorResponseSummary(ScriptScopeContext scope, ResponseStatus errorStatus) =>
            ViewUtils.ErrorResponseSummary(errorStatus);

        public string errorResponseExcept(ScriptScopeContext scope, object fields) =>
            errorResponseExcept(scope, getErrorStatus(scope), fields);
        public string errorResponseExcept(ScriptScopeContext scope, ResponseStatus errorStatus, object fields)
        {
            if (errorStatus == null)
                return null;

            var fieldNames = Context.DefaultScripts.toVarNames(fields);
            return ViewUtils.ErrorResponseExcept(errorStatus, fieldNames);
        }

        public string errorResponse(ScriptScopeContext scope) => errorResponse(scope, getErrorStatus(scope), null);
        public string errorResponse(ScriptScopeContext scope, string fieldName) =>
            errorResponse(scope, getErrorStatus(scope), fieldName);

        public string errorResponse(ScriptScopeContext scope, ResponseStatus errorStatus, string fieldName) =>
            ViewUtils.ErrorResponse(errorStatus, fieldName);

        private static IVirtualPathProvider GetBundleVfs(IVirtualPathProvider virtualFiles, string filterName, bool toDisk)
        {
            var vfs = !toDisk
                ? (virtualFiles as MemoryVirtualFiles ??
                   ((virtualFiles is MultiVirtualFiles memVfs
                        ? memVfs.ChildProviders.FirstOrDefault(x => x is MemoryVirtualFiles)
                        : null) ??
                    throw new NotSupportedException($"MemoryVirtualFiles is required in {filterName} when disk=false")))
                : (virtualFiles as FileSystemVirtualFiles ??
                   ((virtualFiles is MultiVirtualFiles fsVfs
                        ? fsVfs.ChildProviders.FirstOrDefault(x => x is FileSystemVirtualFiles)
                        : null) ??
                    throw new NotSupportedException($"FileSystemVirtualFiles is required in {filterName} when disk=true"))
                );
            return vfs;
        }
        
        public IRawString bundleJs(ScriptScopeContext scope, object virtualPaths) =>
            bundleJs(scope, virtualPaths, null);
        public IRawString bundleJs(ScriptScopeContext scope, object virtualPaths, object options)
        {
            var args = options.AssertOptions(nameof(bundleJs));
            return ViewUtils.BundleJs(nameof(bundleJs),
                Context.VirtualFiles,
                Minifiers.JavaScript,
                new BundleOptions {
                    Sources = ViewUtils.ToStringList((IEnumerable)virtualPaths),
                    OutputTo =  args.TryGetValue("out", out var oOut) ? oOut as string : null,
                    Minify =  !args.TryGetValue("minify", out var oMinify) || oMinify is bool bMinify && bMinify,
                    SaveToDisk = args.TryGetValue("disk", out var oDisk) && oDisk is bool bDisk && bDisk,
                    Cache =  !args.TryGetValue("cache", out var oCache) || oCache is bool bCache && bCache,
                    RegisterModuleInAmd = args.TryGetValue("amd", out var oReg) && oReg is bool bReg && bReg,                    
                }).ToRawString();
        }
        
        public IRawString bundleCss(ScriptScopeContext scope, object virtualPaths) =>
            bundleCss(scope, virtualPaths, null);
        public IRawString bundleCss(ScriptScopeContext scope, object virtualPaths, object options)
        {
            var args = options.AssertOptions(nameof(bundleCss));
            return ViewUtils.BundleCss(nameof(bundleCss),
                Context.VirtualFiles,
                Minifiers.Css,
                new BundleOptions {
                    Sources = ViewUtils.ToStringList((IEnumerable)virtualPaths),
                    OutputTo =  args.TryGetValue("out", out var oOut) ? oOut as string : null,
                    Minify =  !args.TryGetValue("minify", out var oMinify) || oMinify is bool bMinify && bMinify,
                    SaveToDisk = args.TryGetValue("disk", out var oDisk) && oDisk is bool bDisk && bDisk,                    
                    Cache =  !args.TryGetValue("cache", out var oCache) || oCache is bool bCache && bCache,
                }).ToRawString();
        }
        
        public IRawString bundleHtml(ScriptScopeContext scope, object virtualPaths) =>
            bundleHtml(scope, virtualPaths, null);
        public IRawString bundleHtml(ScriptScopeContext scope, object virtualPaths, object options)
        {
            var args = options.AssertOptions(nameof(bundleHtml));
            return ViewUtils.BundleHtml(nameof(bundleHtml),
                Context.VirtualFiles,
                Minifiers.Html,
                new BundleOptions {
                    Sources = ViewUtils.ToStringList((IEnumerable)virtualPaths),
                    OutputTo =  args.TryGetValue("out", out var oOut) ? oOut as string : null,
                    Minify =  !args.TryGetValue("minify", out var oMinify) || oMinify is bool bMinify && bMinify,
                    SaveToDisk = args.TryGetValue("disk", out var oDisk) && oDisk is bool bDisk && bDisk,                    
                    Cache =  !args.TryGetValue("cache", out var oCache) || oCache is bool bCache && bCache,
                }).ToRawString();
        }
    }
}