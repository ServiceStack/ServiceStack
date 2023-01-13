using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Html;
using ServiceStack.IO;
using ServiceStack.Logging;
using ServiceStack.Script;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
    [Obsolete("Use ServiceStackScripts")]
    public class TemplateServiceStackFilters : ServiceStackScripts {}
    
    public partial class ServiceStackScripts : ScriptMethods, IConfigureScriptContext
    {
        public static List<string> RemoveNewLinesFor { get; } = new List<string> {
            nameof(publishToGateway),
        };

        public void Configure(ScriptContext context)
        {
            RemoveNewLinesFor.Each(name => context.RemoveNewLineAfterFiltersNamed.Add(name));
        }
        
        public static ILog Log = LogManager.GetLogger(typeof(ServiceStackScripts));
        
        private ServiceStackHost appHost => HostContext.AppHost;

        public IVirtualFiles vfsContent() => HostContext.VirtualFiles;

        public MemoryVirtualFiles hostVfsMemory() => HostContext.MemoryVirtualFiles;
        public FileSystemVirtualFiles hostVfsFileSystem() => HostContext.FileSystemVirtualFiles;
        public GistVirtualFiles hostVfsGist() => HostContext.GistVirtualFiles;

        public IHttpRequest httpRequest(ScriptScopeContext scope) => scope.GetHttpRequest();

        public object requestItem(ScriptScopeContext scope, string key) => scope.GetRequest().GetItem(key);

        public object baseUrl(ScriptScopeContext scope) => scope.GetRequest().GetBaseUrl();

        public object resolveUrl(ScriptScopeContext scope, string virtualPath) =>
            scope.GetRequest().ResolveAbsoluteUrl(virtualPath);

        public string serviceUrl(ScriptScopeContext scope, string requestName) => 
            serviceUrl(scope, requestName, null, HttpMethods.Get);
        public string serviceUrl(ScriptScopeContext scope, string requestName, Dictionary<string, object> properties) =>
            serviceUrl(scope, requestName, properties, HttpMethods.Get);
        public string serviceUrl(ScriptScopeContext scope, string requestName, Dictionary<string, object> properties, string httpMethod)
        {
            if (requestName == null)
                throw new ArgumentNullException(nameof(requestName));

            var requestType = AssertRequestType(requestName);
            var requestDto = appHost.Metadata.CreateRequestDto(requestType, properties);

            var url = requestDto.ToUrl(httpMethod, "json");
            return url;
        }

        private Type AssertRequestType(string requestName)
        {
            var requestType = appHost.Metadata.GetOperationType(requestName);
            if (requestType == null)
                throw new ArgumentException("Request DTO not found: " + requestName);
            
            return requestType;
        }

        public object execService(ScriptScopeContext scope, string requestName) => 
            sendToGateway(scope, TypeConstants.EmptyObjectDictionary, requestName, null);

        public object execService(ScriptScopeContext scope, string requestName, object options) => 
            sendToGateway(scope, TypeConstants.EmptyObjectDictionary, requestName, options);

        public object sendToGateway(ScriptScopeContext scope, string requestName) => 
            sendToGateway(scope, TypeConstants.EmptyObjectDictionary, requestName, null);
        public object sendToGateway(ScriptScopeContext scope, object dto, string requestName) => 
            sendToGateway(scope, dto, requestName, null);
        public object sendToGateway(ScriptScopeContext scope, object dto, string requestName, object options)
        {
            try
            {
                if (requestName == null)
                    throw new ArgumentNullException(nameof(requestName));
                if (dto == null)
                    throw new ArgumentNullException(nameof(dto));
                
                var gateway = appHost.GetServiceGateway(scope.GetRequest());
                var requestType = AssertRequestType(requestName);

                var responseType = appHost.Metadata.GetResponseTypeByRequest(requestType);

                var requestDto = appHost.Metadata.CreateRequestDto(requestType, dto);

                var response = gateway.Send(responseType, requestDto);
                return response;
            }
            catch (Exception ex)
            {
                if (Log.IsDebugEnabled)
                    Log.Error(ex.Message, ex);
                
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
                var requestDto = CreateRequestDto(dto, requestName);
                var gateway = appHost.GetServiceGateway(scope.GetRequest());
                gateway.Publish(requestDto);
                return StopExecution.Value;
            }
            catch (Exception ex)
            {
                if (Log.IsDebugEnabled)
                    Log.Error(ex.Message, ex);
                
                throw new StopFilterExecutionException(scope, options, ex);
            }
        }

        private object CreateRequestDto(object dto, string requestName)
        {
            if (requestName == null)
                throw new ArgumentNullException(nameof(requestName));
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            var requestType = appHost.Metadata.GetOperationType(requestName);
            if (requestType == null)
                throw new ArgumentException("Request DTO not found: " + requestName);

            var requestDto = dto.GetType() == requestType
                ? dto
                : dto is Dictionary<string, object> objDictionary
                    ? objDictionary.FromObjectDictionary(requestType)
                    : dto.ConvertTo(requestType);
            return requestDto;
        }

        public IgnoreResult publishMessage(ScriptScopeContext scope, string requestName, object dto) =>
            publishMessage(scope, requestName, dto, null);
        public IgnoreResult publishMessage(ScriptScopeContext scope, string requestName, object dto, object options)
        {
            var msgProducer = appHost.GetMessageProducer();
            if (msgProducer == null)
                throw new NotSupportedException("IMessageService not configured");
            
            try 
            {
                var requestDto = CreateRequestDto(dto, requestName);
                using (msgProducer)
                {
                    appHost.PublishMessage(msgProducer, requestDto);
                    return IgnoreResult.Value;
                }
            }
            catch (Exception ex)
            {
                if (Log.IsDebugEnabled)
                    Log.Error(ex.Message, ex);
                
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
                    if (Context.ScriptMethods.FirstOrDefault(x => x is IAutoQueryDbFilters) is not IAutoQueryDbFilters ssFilter)
                        throw new NotImplementedException(nameof(sendToAutoQuery) + " RDBMS requires AutoQueryScripts");

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
                
                var httpReq = scope.GetRequest();
                var ctx = autoQuery.CreateContext(aqDto, reqParams, httpReq);
                var fromType = autoQuery.GetFromType(aqDto.GetType());
                using var db = autoQuery.GetDb(ctx, fromType);
                var q = autoQuery.CreateQuery(aqDto, reqParams, httpReq, db);
                var response = autoQuery.Execute(aqDto, q, db);

                return response;
            }
            catch (Exception ex)
            {
                if (Log.IsDebugEnabled)
                    Log.Error(ex.Message, ex);
                
                throw new StopFilterExecutionException(scope, options, ex);
            }
        }

        public object toResults(object dto)
        {
            var results = AutoQueryDataServiceSource.GetResults(dto);
            return results;
        }
       
        public object getUserSession(ScriptScopeContext scope) => scope.GetRequest().GetSession();
        public IAuthSession userSession(ScriptScopeContext scope) => scope.GetRequest().GetSession();
        public IAuthSession sessionIfAuthenticated(ScriptScopeContext scope)
        {
            var session = scope.GetRequest().GetSession();
            return session.IsAuthenticated
                ? session
                : null;
        }

        public string userAuthId(ScriptScopeContext scope) => scope.GetRequest().GetSession()?.UserAuthId;
        public int? userAuthIntId(ScriptScopeContext scope) => scope.GetRequest().GetSession()?.UserAuthId.ToInt();
        public string userAuthName(ScriptScopeContext scope)
        {
            var authSession = scope.GetRequest().GetSession();
            return authSession?.UserAuthName ?? authSession?.UserName ?? authSession?.Email;
        }

        public string userProfileUrl(ScriptScopeContext scope) => scope.GetRequest().GetSession().GetProfileUrl();

        public HashSet<string> userAttributes(ScriptScopeContext scope) => scope.GetRequest().GetUserAttributes();

        public bool isAuthenticated(ScriptScopeContext scope)
        {
            var request = scope.GetRequest();
#pragma warning disable CS0618
            return request != null && AuthenticateAttribute.Authenticate(request, request.GetSession());
#pragma warning restore CS0618
        }

        public bool isAuthenticated(ScriptScopeContext scope, string provider)
        {
            var request = scope.GetRequest();
#pragma warning disable CS0618
            return request != null && AuthenticateAttribute.Authenticate(request);
#pragma warning restore CS0618
        }

        public object redirectTo(ScriptScopeContext scope, string path)
        {
            return Context.DefaultMethods.@return(scope, new HttpResult(null, null, HttpStatusCode.Redirect) {
                Headers = {
                    [HttpHeaders.Location] = path.FirstCharEquals('~')
                        ? scope.GetRequest().ResolveAbsoluteUrl(path)
                        : path
                }
            });
        }

        public object redirectIfNotAuthenticated(ScriptScopeContext scope)
        {
            if (!isAuthenticated(scope))
            {
                var url = HostContext.AssertPlugin<AuthFeature>().GetHtmlRedirectUrl(scope.GetRequest());
                return redirectTo(scope, url);
            }
            return IgnoreResult.Value;
        }

        public object redirectIfNotAuthenticated(ScriptScopeContext scope, string path)
        {
            if (!isAuthenticated(scope))
            {
                var url = HostContext.AssertPlugin<AuthFeature>().GetHtmlRedirectUrl(scope.GetRequest(), path, includeRedirectParam:true);
                return redirectTo(scope, url);
            }
            
            return IgnoreResult.Value;
        }

        public object hasRole(ScriptScopeContext scope, string role) =>
            userSession(scope)?.HasRole(role, scope.GetRequest().TryResolve<IAuthRepository>()) == true;

        public object hasPermission(ScriptScopeContext scope, string permission) =>
            userSession(scope)?.HasPermission(permission, scope.GetRequest().TryResolve<IAuthRepository>()) == true;

        public object assertRole(ScriptScopeContext scope, string role) => assertRole(scope, role, null);
        public object assertRole(ScriptScopeContext scope, string role, Dictionary<string,object> options)
        {
            var args = scope.AssertOptions(nameof(assertRole), options);
            if (redirectIfNotAuthenticated(scope) is StopExecution ret)
                return ret;
            
            var authSession = userSession(scope);
            if (!authSession.HasRole(role, scope.GetRequest().TryResolve<IAuthRepository>()))
            {
                if (args.TryGetValue("redirect", out var oRedirect))
                {
                    var path = (string)oRedirect;
                    return redirectTo(scope, path);
                }

                var message = args.TryGetValue("message", out var oMessage)
                    ? (string) oMessage
                    : ErrorMessages.InvalidRole.Localize(scope.GetRequest());
                    
                return Context.DefaultMethods.@return(scope, new HttpError(HttpStatusCode.Forbidden, message));
            }

            return IgnoreResult.Value;
        }

        public object assertPermission(ScriptScopeContext scope, string permission) => assertRole(scope, permission, null);
        public object assertPermission(ScriptScopeContext scope, string permission, Dictionary<string,object> options)
        {
            var args = scope.AssertOptions(nameof(permission), options);
            if (redirectIfNotAuthenticated(scope) is StopExecution ret)
                return ret;
            
            var authSession = userSession(scope);
            if (!authSession.HasPermission(permission, scope.GetRequest().TryResolve<IAuthRepository>()))
            {
                if (args.TryGetValue("redirect", out var oRedirect))
                {
                    var path = (string)oRedirect;
                    return redirectTo(scope, path);
                }

                var message = args.TryGetValue("message", out var oMessage)
                    ? (string) oMessage
                    : ErrorMessages.InvalidRole.Localize(scope.GetRequest());
                    
                return Context.DefaultMethods.@return(scope, new HttpError(HttpStatusCode.Forbidden, message));
            }
            return IgnoreResult.Value;
        }

        public object ifAuthenticated(ScriptScopeContext scope) => isAuthenticated(scope) 
            ? (object)IgnoreResult.Value : StopExecution.Value;
       
        public object ifNotAuthenticated(ScriptScopeContext scope) => !isAuthenticated(scope) 
            ? (object)IgnoreResult.Value : StopExecution.Value;
        
        public object onlyIfAuthenticated(ScriptScopeContext scope, object value) => isAuthenticated(scope) 
            ? value : StopExecution.Value;

        public object endIfAuthenticated(ScriptScopeContext scope, object value) => !isAuthenticated(scope) 
            ? value : StopExecution.Value;

        public IAuthRepository authRepo(ScriptScopeContext scope) => HostContext.AppHost.GetAuthRepository(scope.GetRequest());

        public IUserAuth newUserAuth(IAuthRepository authRepo) =>
            authRepo is ICustomUserAuth c ? c.CreateUserAuth() : new UserAuth();

        public IUserAuthDetails newUserAuthDetails(IAuthRepository authRepo) =>
            authRepo is ICustomUserAuth c ? c.CreateUserAuthDetails() : new UserAuthDetails();

        public IUserAuth getUserAuth(IAuthRepository authRepo, string userAuthId) =>
            authRepo.GetUserAuth(userAuthId);

        public IUserAuth getUserAuthByUserName(IAuthRepository authRepo, string userNameOrEmail) =>
            authRepo.GetUserAuthByUserName(userNameOrEmail);

        public IUserAuth tryAuthenticate(ScriptScopeContext scope, IAuthRepository authRepo, string userName, string password) =>
            authRepo.TryAuthenticate(userName, password, out var ret) ? ret : null;

        public IUserAuth createUserAuth(IAuthRepository authRepo, IUserAuth newUser, string password) =>
            authRepo.CreateUserAuth(newUser, password);

        public IgnoreResult saveUserAuth(IAuthRepository authRepo, IUserAuth userAuth)
        {
            authRepo.SaveUserAuth(userAuth);
            return IgnoreResult.Value;
        }

        public IUserAuth updateUserAuth(IAuthRepository authRepo, IUserAuth existingUser, IUserAuth newUser) =>
            authRepo.UpdateUserAuth(existingUser, newUser);

        public IgnoreResult updateUserAuth(IAuthRepository authRepo, IUserAuth existingUser, IUserAuth newUser, string password)
        {
            authRepo.UpdateUserAuth(existingUser, newUser, password);
            return IgnoreResult.Value;
        }

        public IgnoreResult deleteUserAuth(IAuthRepository authRepo, string userAuthId)
        {
            authRepo.DeleteUserAuth(userAuthId);
            return IgnoreResult.Value;
        }

        public List<IUserAuth> getUserAuths(IAuthRepository authRepo) => getUserAuths(authRepo, null);
        public List<IUserAuth> getUserAuths(IAuthRepository authRepo, Dictionary<string, object> options)
        {
            var opt = options ?? TypeConstants.EmptyObjectDictionary;

            return authRepo.GetUserAuths(
                orderBy: opt.TryGetValue("orderBy", out var oOrderBy) ? (string) oOrderBy : null,
                skip: opt.TryGetValue("skip", out var oSkip) ? (int) oSkip : (int?)null,
                take: opt.TryGetValue("take", out var oTake) ? (int) oTake : (int?)null
            );
        }

        public List<IUserAuth> searchUserAuths(IAuthRepository authRepo, Dictionary<string, object> options)
        {
            var opt = options ?? TypeConstants.EmptyObjectDictionary;

            return authRepo.SearchUserAuths(
                query: opt.TryGetValue("query", out var oQuery) ? (string) oQuery: null,
                orderBy: opt.TryGetValue("orderBy", out var oOrderBy) ? (string) oOrderBy : null,
                skip: opt.TryGetValue("skip", out var oSkip) ? (int) oSkip : (int?)null,
                take: opt.TryGetValue("take", out var oTake) ? (int) oTake : (int?)null
            );
        }

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

        public ResponseStatus getErrorStatus(ScriptScopeContext scope) => scope.GetErrorStatus();

        /// <summary>
        /// Only return form input value if form submission was invalid
        /// </summary>
        public string formValue(ScriptScopeContext scope, string name) => formValue(scope, name, null);

        public string formValue(ScriptScopeContext scope, string name, string defaultValue) => hasErrorStatus(scope) 
            ? ViewUtils.FormQuery(scope.GetRequest(), name) 
            : defaultValue;

        public string[] formValues(ScriptScopeContext scope, string name) => hasErrorStatus(scope) 
            ? ViewUtils.FormQueryValues(scope.GetRequest(), name) 
            : null;
    
        public bool formCheckValue(ScriptScopeContext scope, string name)
        {
            var value = formValue(scope, name);
            return value == "true" || value == "True" || value == "t" || value == "on" || value == "1";
        }
        
        public string errorResponseSummary(ScriptScopeContext scope) => errorResponseSummary(scope, getErrorStatus(scope));

        public string errorResponseSummary(ScriptScopeContext scope, ResponseStatus errorStatus) =>
            ViewUtils.ErrorResponseSummary(errorStatus);

        public string errorResponseExcept(ScriptScopeContext scope, IEnumerable fields) =>
            errorResponseExcept(scope, getErrorStatus(scope), fields);
        public string errorResponseExcept(ScriptScopeContext scope, ResponseStatus errorStatus, IEnumerable fields)
        {
            if (errorStatus == null)
                return null;

            var fieldNames = Context.DefaultMethods.toVarNames(fields);
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
        
        public IRawString bundleJs(object virtualPaths) => bundleJs(virtualPaths, null);
        public IRawString bundleJs(object virtualPaths, object options)
        {
            var args = options.AssertOptions(nameof(bundleJs));
            return ViewUtils.BundleJs(nameof(bundleJs),
                Context.VirtualFiles,
                HostContext.VirtualFiles,
                Minifiers.JavaScript,
                new BundleOptions {
                    Sources = ViewUtils.ToStringList((IEnumerable) virtualPaths),
                    OutputTo = args.TryGetValue("out", out var oOut) ? oOut as string : null,
                    OutputWebPath = args.TryGetValue("outWebPath", out var oOutWebPath) ? oOutWebPath as string : null,
                    PathBase = args.TryGetValue("pathBase", out var oPathBase) ? oPathBase as string : HostContext.Config.PathBase,
                    Minify = !args.TryGetValue("minify", out var oMinify) || oMinify is bool bMinify && bMinify,
                    SaveToDisk = args.TryGetValue("disk", out var oDisk) && oDisk is bool bDisk && bDisk,
                    Cache = !args.TryGetValue("cache", out var oCache) || oCache is bool bCache && bCache,
                    Bundle = !args.TryGetValue("bundle", out var oBundle) || oBundle is bool bBundle && bBundle,
                    RegisterModuleInAmd = args.TryGetValue("amd", out var oReg) && oReg is bool bReg && bReg,
                    IIFE = args.TryGetValue("iife", out var oIife) && oIife is bool bIife && bIife,
                }).ToRawString();
        }

        public IRawString bundleCss(object virtualPaths) => bundleCss(virtualPaths, null);
        public IRawString bundleCss(object virtualPaths, object options) 
        {
            var args = options.AssertOptions(nameof(bundleCss));
            return ViewUtils.BundleCss(nameof(bundleCss),
                Context.VirtualFiles,
                HostContext.VirtualFiles,
                Minifiers.Css,
                new BundleOptions {
                    Sources = ViewUtils.ToStringList((IEnumerable) virtualPaths),
                    OutputTo = args.TryGetValue("out", out var oOut) ? oOut as string : null,
                    OutputWebPath = args.TryGetValue("outWebPath", out var oOutWebPath) ? oOutWebPath as string : null,
                    PathBase = args.TryGetValue("pathBase", out var oPathBase) ? oPathBase as string : HostContext.Config.PathBase,
                    Minify = !args.TryGetValue("minify", out var oMinify) || oMinify is bool bMinify && bMinify,
                    SaveToDisk = args.TryGetValue("disk", out var oDisk) && oDisk is bool bDisk && bDisk,
                    Cache = !args.TryGetValue("cache", out var oCache) || oCache is bool bCache && bCache,
                    Bundle = !args.TryGetValue("bundle", out var oBundle) || oBundle is bool bBundle && bBundle,
                }).ToRawString();
        }

        public IRawString bundleHtml(object virtualPaths) => bundleHtml(virtualPaths, null);
        public IRawString bundleHtml(object virtualPaths, object options) 
        {
            var args = options.AssertOptions(nameof(bundleHtml));
            return ViewUtils.BundleHtml(nameof(bundleHtml),
                Context.VirtualFiles,
                HostContext.VirtualFiles,
                Minifiers.Html,
                new BundleOptions {
                    Sources = ViewUtils.ToStringList((IEnumerable) virtualPaths),
                    OutputTo = args.TryGetValue("out", out var oOut) ? oOut as string : null,
                    OutputWebPath = args.TryGetValue("outWebPath", out var oOutWebPath) ? oOutWebPath as string : null,
                    PathBase = args.TryGetValue("pathBase", out var oPathBase) ? oPathBase as string : HostContext.Config.PathBase,
                    Minify = !args.TryGetValue("minify", out var oMinify) || oMinify is bool bMinify && bMinify,
                    SaveToDisk = args.TryGetValue("disk", out var oDisk) && oDisk is bool bDisk && bDisk,
                    Cache = !args.TryGetValue("cache", out var oCache) || oCache is bool bCache && bCache,
                    Bundle = !args.TryGetValue("bundle", out var oBundle) || oBundle is bool bBundle && bBundle,
                }).ToRawString();
        }

        public IRawString serviceStackLogoSvg(string color) => Svg.Fill(Svg.GetImage(Svg.Logos.ServiceStack),color).ToRawString();
        public IRawString serviceStackLogoSvg() => Svg.GetImage(Svg.Logos.ServiceStack).ToRawString();
        public IRawString serviceStackLogoDataUri(string color) => Svg.Fill(Svg.GetDataUri(Svg.Logos.ServiceStack),color).ToRawString();
        public IRawString serviceStackLogoDataUri() => Svg.GetDataUri(Svg.Logos.ServiceStack).ToRawString();
        public IRawString serviceStackLogoDataUriLight() => serviceStackLogoDataUri(Svg.LightColor);

        public IRawString svgImage(string name) => Svg.GetImage(name).ToRawString();
        public IRawString svgImage(string name, string fillColor) => Svg.GetImage(name, fillColor).ToRawString();
        public IRawString svgDataUri(string name) => Svg.GetDataUri(name).ToRawString();
        public IRawString svgDataUri(string name, string fillColor) => Svg.GetDataUri(name, fillColor).ToRawString();

        public IRawString svgBackgroundImageCss(string name) => Svg.GetBackgroundImageCss(name).ToRawString();
        public IRawString svgBackgroundImageCss(string name, string fillColor) => Svg.GetBackgroundImageCss(name, fillColor).ToRawString();
        public IRawString svgInBackgroundImageCss(string svg) => Svg.InBackgroundImageCss(svg).ToRawString();

        public IRawString svgFill(string svg, string color) => Svg.Fill(svg, color).ToRawString();

        public string svgBaseUrl(ScriptScopeContext scope) => scope.GetRequest().ResolveAbsoluteUrl(HostContext.AssertPlugin<SvgFeature>().RoutePath);

        public Dictionary<string, string> svgImages() => Svg.Images;
        public Dictionary<string, string> svgDataUris() => Svg.DataUris;

        public Dictionary<string, List<string>> svgCssFiles() => Svg.CssFiles;

        public IgnoreResult svgAdd(string svg, string name)
        {
            Svg.AddImage(svg, name);
            return IgnoreResult.Value;
        }

        public IgnoreResult svgAdd(string svg, string name, string cssFile)
        {
            Svg.AddImage(svg, name, cssFile);
            return IgnoreResult.Value;
        }

        public IgnoreResult svgAddFile(ScriptScopeContext scope, string svgPath, string name)
        {
            var svg = (scope.Context.VirtualFiles.GetFile(svgPath) ?? throw new FileNotFoundException(svgPath)).ReadAllText();
            Svg.AddImage(svg, name);
            return IgnoreResult.Value;
        }

        public IgnoreResult svgAddFile(ScriptScopeContext scope, string svgPath, string name, string cssFile)
        {
            var svgFile = scope.Context.VirtualFiles.GetFile(svgPath) ?? throw new FileNotFoundException(svgPath);
            var svg = svgFile.ReadAllText();
            Svg.AddImage(svg, name, cssFile);
            return IgnoreResult.Value;
        }
    }

    public class SvgScriptBlock : ScriptBlock
    {
        public override string Name => "svg";
        public override ScriptLanguage Body => ScriptTemplate.Language;
        public override async Task WriteAsync(ScriptScopeContext scope, PageBlockFragment block, CancellationToken token)
        {
            if (block.Argument.IsEmpty)
                throw new NotSupportedException($"Name required in {Name} script block");
            
            var argumentStr = block.Argument.ToString();
            var args = argumentStr.SplitOnFirst(' ');
            var name = args[0].Trim();

            using var ms = MemoryStreamFactory.GetStream();
            var useScope = scope.ScopeWithStream(ms);
            await WriteBodyAsync(useScope, block, token);

            var capturedSvg = await ms.ReadToEndAsync();                
            Svg.AddImage(capturedSvg, name, args.Length == 2 ? args[1].Trim() : null);
        }
    }
    
    public abstract class MinifyScriptBlockBase : ScriptBlock
    {
        public abstract ICompressor Minifier { get; }
        public override ScriptLanguage Body => ScriptVerbatim.Language;

        //reduce string allocation of block contents at runtime
        readonly ConcurrentDictionary<ReadOnlyMemory<char>, Tuple<string,string>> allocatedStringsCache = 
            new ConcurrentDictionary<ReadOnlyMemory<char>, Tuple<string,string>>();

        public ReadOnlyMemory<char> GetMinifiedOutputCache(ReadOnlyMemory<char> contents)
        {
            if (Context.DebugMode)
                return contents;
            
            var cachedStrings = allocatedStringsCache.GetOrAdd(contents, c => {
                    var str = c.ToString();
                    return Tuple.Create(Name + ":" + str, str); //cache allocated key + string
                });

            var minified = (ReadOnlyMemory<char>) Context.Cache.GetOrAdd(cachedStrings.Item1, k => 
                Minifier.Compress(cachedStrings.Item2).AsMemory());
            
            Context.Cache[cachedStrings.Item1] = minified;
            return minified;
        }
        
        public override async Task WriteAsync(ScriptScopeContext scope, PageBlockFragment block, CancellationToken token)
        {
            if (block.Body.Length == 0)
                return;
            
            var strFragment = (PageStringFragment)block.Body[0];

            if (!block.Argument.IsNullOrWhiteSpace())
            {
                Capture(scope, block, strFragment);
            }
            else
            {
                var minified = GetMinifiedOutputCache(strFragment.Value);
                await scope.OutputStream.WriteAsync(minified.Span, token);
            }
        }

        private void Capture(ScriptScopeContext scope, PageBlockFragment block, PageStringFragment strFragment)
        {
            var literal = block.Argument.Span.AdvancePastWhitespace();
            bool appendTo = false;
            if (literal.StartsWith("appendTo "))
            {
                appendTo = true;
                literal = literal.Advance("appendTo ".Length);
            }

            var minified = GetMinifiedOutputCache(strFragment.Value);

            literal = literal.ParseVarName(out var name);
            var nameString = name.Value();
            if (appendTo && scope.PageResult.Args.TryGetValue(nameString, out var oVar)
                         && oVar is string existingString)
            {
                scope.PageResult.Args[nameString] = existingString + minified;
                return;
            }

            scope.PageResult.Args[nameString] = minified.ToString();
        }
    }

    public class MinifyJsScriptBlock : MinifyScriptBlockBase
    {
        public override string Name => "minifyjs";
        public override ICompressor Minifier => Minifiers.JavaScript;
    }

    public class MinifyCssScriptBlock : MinifyScriptBlockBase
    {
        public override string Name => "minifycss";
        public override ICompressor Minifier => Minifiers.Css;
    }

    public class MinifyHtmlScriptBlock : MinifyScriptBlockBase
    {
        public override string Name => "minifyhtml";
        public override ICompressor Minifier => Minifiers.Html;
    }    
    
    public class ServiceStackScriptBlocks : IScriptPlugin
    {
        public void Register(ScriptContext context)
        {
            context.ScriptBlocks.AddRange(new ScriptBlock[] {
                new MinifyJsScriptBlock(), 
                new MinifyCssScriptBlock(), 
                new MinifyHtmlScriptBlock(), 
                new SvgScriptBlock(), 
            });
        }
    }

    public static class ServiceStackScriptUtils
    {
        public static HashSet<string> GetUserAttributes(this IRequest request)
        {
            if (request == null)
                return TypeConstants<string>.EmptyHashSet;
            
            if (request.Items.TryGetValue(Keywords.Attributes, out var oAttrs))
                return (HashSet<string>)oAttrs;
                
            var authSession = request.GetSession();
            var attrs = new HashSet<string>();
            if (authSession?.IsAuthenticated == true)
            {
                attrs.Add(When.IsAuthenticated);
                
                if (HostContext.HasValidAuthSecret(request))
                    attrs.Add(RoleNames.Admin);

                var roles = authSession.Roles;
                var permissions = authSession.Permissions;
                
                if (roles.IsEmpty() && permissions.IsEmpty())
                {
                    var authRepo = HostContext.AppHost.GetAuthRepository(request);
                    using (authRepo as IDisposable)
                    {
                        if (authRepo is IManageRoles manageRoles)
                        {
                            manageRoles.GetRolesAndPermissions(authSession.UserAuthId, out var iroles, out var ipermissions);
                            roles = iroles.ToList();
                            permissions = ipermissions.ToList();
                        }
                    }
                }
                
                if (roles != null)
                {
                    foreach (var role in roles)
                    {
                        attrs.Add(When.HasRole(role));
                    }
                }
                if (permissions != null)
                {
                    foreach (var perm in permissions)
                    {
                        attrs.Add(When.HasPermission(perm));
                    }
                }
                
                if (authSession is IAuthSessionExtended extended)
                {
                    if (extended.Scopes != null)
                    {
                        foreach (var item in extended.Scopes)
                        {
                            attrs.Add(When.HasScope(item));
                        }
                    }
                }
                var claims = request.GetClaims();
                if (claims != null)
                {
                    foreach (var claim in claims)
                    {
                        attrs.Add(When.HasClaim(claim.ToString()));
                    }
                }
            }
            request.Items[Keywords.Attributes] = attrs;
            
            return attrs;            
        }
        
        public static NavOptions WithDefaults(this NavOptions options, IRequest request)
        {
            options ??= new NavOptions();
            options.ActivePath ??= request.PathInfo;
            options.Attributes ??= request.GetUserAttributes();
            var pathBase = HostContext.Config.PathBase;
            if (!string.IsNullOrEmpty(pathBase))
                options.BaseHref = pathBase;
                
            return options;
        }

        public static IRequest GetRequest(this ScriptScopeContext scope) =>
            scope.GetValue(ScriptConstants.Request) as IRequest;
        
        public static IHttpRequest GetHttpRequest(this ScriptScopeContext scope) =>
            scope.GetValue(ScriptConstants.Request) as IHttpRequest;
        
        public static string ResolveUrl(this ScriptScopeContext scope, string url) =>
            scope.GetRequest().ResolveAbsoluteUrl(url);
        
        public static ResponseStatus GetErrorStatus(this ScriptScopeContext scope) =>
            scope.GetValue("errorStatus") as ResponseStatus ??
            ViewUtils.GetErrorStatus(scope.GetRequest());
    }

}