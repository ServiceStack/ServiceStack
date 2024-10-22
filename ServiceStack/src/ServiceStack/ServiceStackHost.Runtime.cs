// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.Host;
using ServiceStack.Host.Handlers;
using ServiceStack.IO;
using ServiceStack.Messaging;
using ServiceStack.Metadata;
using ServiceStack.MiniProfiler;
using ServiceStack.Model;
using ServiceStack.Redis;
using ServiceStack.Serialization;
using ServiceStack.Support.WebHost;
using ServiceStack.Text;
using ServiceStack.Web;
using System.Runtime.CompilerServices;
#if NETFRAMEWORK
using System.Web;
#endif


namespace ServiceStack;

public abstract partial class ServiceStackHost
{
    /// <summary>
    /// Executes Service Request Converters
    /// </summary>
    public async Task<object> ApplyRequestConvertersAsync(IRequest req, object requestDto)
    {
        foreach (var converter in RequestConvertersArray)
        {
            requestDto = await converter(req, requestDto).ConfigAwaitNetCore() ?? requestDto;
            if (req.Response.IsClosed)
                return requestDto;
        }

        return requestDto;
    }

    /// <summary>
    /// Executes Service Response Converters
    /// </summary>
    public async Task<object> ApplyResponseConvertersAsync(IRequest req, object responseDto)
    {
        foreach (var converter in ResponseConvertersArray)
        {
            responseDto = await converter(req, responseDto).ConfigAwaitNetCore() ?? responseDto;
            if (req.Response.IsClosed)
                return responseDto;
        }

        return responseDto;
    }

    /// <summary>
    /// Apply PreRequest Filters for participating Custom Handlers, e.g. RazorFormat, MarkdownFormat, etc
    /// </summary>
    public bool ApplyCustomHandlerRequestFilters(IRequest httpReq, IResponse httpRes)
    {
        return ApplyPreRequestFilters(httpReq, httpRes);
    }

    /// <summary>
    /// Apply PreAuthenticate Filters from IAuthWithRequest AuthProviders
    /// </summary>
    public virtual async Task ApplyPreAuthenticateFiltersAsync(IRequest httpReq, IResponse httpRes)
    {
        httpReq.Items[Keywords.HasPreAuthenticated] = bool.TrueString;
            
        foreach (var authProvider in AuthenticateService.AuthWithRequestProviders.Safe())
        {
            await authProvider.PreAuthenticateAsync(httpReq, httpRes).ConfigAwaitNetCore();
            if (httpRes.IsClosed)
                return;
        }
        foreach (var authProvider in AuthenticateService.AuthWithRequestSyncProviders.Safe())
        {
            authProvider.PreAuthenticate(httpReq, httpRes);
            if (httpRes.IsClosed)
                return;
        }
    }

    /// <summary>
    /// Applies the raw request filters. Returns whether or not the request has been handled 
    /// and no more processing should be done.
    /// </summary>
    /// <returns></returns>
    public bool ApplyPreRequestFilters(IRequest httpReq, IResponse httpRes)
    {
        if (PreRequestFiltersArray.Length == 0)
            return false;

        using (Profiler.Current.Step("Executing Pre RequestFilters"))
        {
            foreach (var requestFilter in PreRequestFiltersArray)
            {
                requestFilter(httpReq, httpRes);
                if (httpRes.IsClosed) break;
            }

            return httpRes.IsClosed;
        }
    }

    [Obsolete("Use ApplyRequestFiltersAsync")]
    public bool ApplyRequestFilters(IRequest req, IResponse res, object requestDto)
    {
        ApplyRequestFiltersAsync(req, res, requestDto).Wait();
        return res.IsClosed;
    }

    /// <summary>
    /// Applies the request filters. Returns whether or not the request has been handled 
    /// and no more processing should be done.
    /// </summary>
    /// <returns></returns>
    public async Task ApplyRequestFiltersAsync(IRequest req, IResponse res, object requestDto)
    {
        if (req == null) throw new ArgumentNullException(nameof(req));
        if (res == null) throw new ArgumentNullException(nameof(res));

        if (res.IsClosed)
            return;

        using var step = Profiler.Current.Step("Executing Request Filters Async");
        if (!req.IsMultiRequest())
        {
            await ApplyRequestFiltersSingleAsync(req, res, requestDto).ConfigAwaitNetCore();
            HostContext.AppHost.OnAfterAwait(req);
            return;
        }

        var dtos = (IEnumerable)requestDto;
        var i = 0;

        foreach (var dto in dtos)
        {
            req.Items[Keywords.AutoBatchIndex] = i;
            await ApplyRequestFiltersSingleAsync(req, res, dto).ConfigAwaitNetCore();
            HostContext.AppHost.OnAfterAwait(req);
            if (res.IsClosed)
                return;

            i++;
        }

        req.Items.Remove(Keywords.AutoBatchIndex);
    }

    /// <summary>
    /// Executes Service Request Filters
    /// </summary>
    protected async Task ApplyRequestFiltersSingleAsync(IRequest req, IResponse res, object requestDto)
    {
        //Exec all RequestFilter attributes with Priority < 0
        var attributes = FilterAttributeCache.GetRequestFilterAttributes(requestDto.GetType());
        var i = 0;
        for (; i < attributes.Length && attributes[i].Priority < 0; i++)
        {
            var attribute = attributes[i];
            Container.AutoWire(attribute);
            if (attribute is IHasRequestFilter filterSync)
            {
                filterSync.RequestFilter(req, res, requestDto);
            }
            else if (attribute is IHasRequestFilterAsync filterAsync)
            {
                await filterAsync.RequestFilterAsync(req, res, requestDto).ConfigAwaitNetCore();
                HostContext.AppHost.OnAfterAwait(req);
            }

            Release(attribute);
            if (res.IsClosed) 
                return;
        }

        if (GlobalTypedRequestFilters.Count > 0)
        {
            ExecTypedFilters(GlobalTypedRequestFilters, req, res, requestDto);
            if (res.IsClosed)
                return;
        }
        if (GlobalTypedRequestFiltersAsync.Count > 0)
        {
            await ExecTypedFiltersAsync(GlobalTypedRequestFiltersAsync, req, res, requestDto);
            if (res.IsClosed)
                return;
        }

        //Exec global filters
        foreach (var requestFilter in GlobalRequestFiltersArray)
        {
            requestFilter(req, res, requestDto);
            if (res.IsClosed) 
                return;
        }
            
        foreach (var requestFilter in GlobalRequestFiltersAsyncArray)
        {
            await requestFilter(req, res, requestDto).ConfigAwaitNetCore();
            HostContext.AppHost.OnAfterAwait(req);
            if (res.IsClosed) 
                return;
        }

        //Exec remaining RequestFilter attributes with Priority >= 0
        for (; i < attributes.Length && attributes[i].Priority >= 0; i++)
        {
            var attribute = attributes[i];
            Container.AutoWire(attribute);

            if (attribute is IHasRequestFilter filterSync)
            {
                filterSync.RequestFilter(req, res, requestDto);
            }
            else if (attribute is IHasRequestFilterAsync filterAsync)
            {
                await filterAsync.RequestFilterAsync(req, res, requestDto).ConfigAwaitNetCore();
                HostContext.AppHost.OnAfterAwait(req);
            }

            Release(attribute);
            if (res.IsClosed) 
                return;
        }
    }

    [Obsolete("Use ApplyResponseFiltersAsync")]
    public bool ApplyResponseFilters(IRequest req, IResponse res, object response)
    {
        ApplyResponseFiltersAsync(req, res, response).Wait();
        return res.IsClosed;
    }
        
    /// <summary>
    /// Applies the response filters. Returns whether or not the request has been handled 
    /// and no more processing should be done.
    /// </summary>
    /// <returns></returns>
    public async Task ApplyResponseFiltersAsync(IRequest req, IResponse res, object response)
    {
        if (req == null) throw new ArgumentNullException(nameof(req));
        if (res == null) throw new ArgumentNullException(nameof(res));

        if (res.IsClosed)
            return;

        using var step = Profiler.Current.Step("Executing Request Filters Async");
        var batchResponse = req.IsMultiRequest() ? response as IEnumerable : null;
        if (batchResponse == null)
        {
            await ApplyResponseFiltersSingleAsync(req, res, response).ConfigAwaitNetCore();
            HostContext.AppHost.OnAfterAwait(req);
            return;
        }

        var i = 0;

        foreach (var dto in batchResponse)
        {
            req.Items[Keywords.AutoBatchIndex] = i;

            await ApplyResponseFiltersSingleAsync(req, res, dto).ConfigAwaitNetCore();
            HostContext.AppHost.OnAfterAwait(req);
            if (res.IsClosed)
                return;

            i++;
        }

        req.Items.Remove(Keywords.AutoBatchIndex);
    }

    /// <summary>
    /// Executes Service Response Filters
    /// </summary>
    protected async Task ApplyResponseFiltersSingleAsync(IRequest req, IResponse res, object response)
    {
        var attributes = req.Dto != null
            ? FilterAttributeCache.GetResponseFilterAttributes(req.Dto.GetType())
            : null;

        //Exec all ResponseFilter attributes with Priority < 0
        var i = 0;
        if (attributes != null)
        {
            for (; i < attributes.Length && attributes[i].Priority < 0; i++)
            {
                var attribute = attributes[i];
                Container.AutoWire(attribute);

                if (attribute is IHasResponseFilter filterSync)
                {
                    filterSync.ResponseFilter(req, res, response);
                }
                else if (attribute is IHasResponseFilterAsync filterAsync)
                {
                    await filterAsync.ResponseFilterAsync(req, res, response).ConfigAwaitNetCore();
                    HostContext.AppHost.OnAfterAwait(req);
                }

                Release(attribute);
                if (res.IsClosed) 
                    return;
            }
        }

        if (response != null)
        {
            if (GlobalTypedResponseFilters.Count > 0)
            {
                ExecTypedFilters(GlobalTypedResponseFilters, req, res, response);
                if (res.IsClosed) 
                    return;
            }
            if (GlobalTypedResponseFiltersAsync.Count > 0)
            {
                await ExecTypedFiltersAsync(GlobalTypedResponseFiltersAsync, req, res, response);
                if (res.IsClosed) 
                    return;
            }
        }

        //Exec global filters
        foreach (var responseFilter in GlobalResponseFiltersArray)
        {
            responseFilter(req, res, response);
            if (res.IsClosed) 
                return;
        }

        foreach (var responseFilter in GlobalResponseFiltersAsyncArray)
        {
            await responseFilter(req, res, response).ConfigAwaitNetCore();
            HostContext.AppHost.OnAfterAwait(req);
            if (res.IsClosed) 
                return;
        }

        //Exec remaining RequestFilter attributes with Priority >= 0
        if (attributes != null)
        {
            for (; i < attributes.Length; i++)
            {
                var attribute = attributes[i];
                Container.AutoWire(attribute);

                if (attribute is IHasResponseFilter filterSync)
                {
                    filterSync.ResponseFilter(req, res, response);
                }
                else if (attribute is IHasResponseFilterAsync filterAsync)
                {
                    await filterAsync.ResponseFilterAsync(req, res, response).ConfigAwaitNetCore();
                    HostContext.AppHost.OnAfterAwait(req);
                }

                Release(attribute);
                if (res.IsClosed) 
                    return;
            }
        }
    }

    /// <summary>
    /// Executes MQ Response Filters
    /// </summary>
    public bool ApplyMessageRequestFilters(IRequest req, IResponse res, object requestDto)
    {
        ExecTypedFilters(GlobalTypedMessageRequestFilters, req, res, requestDto);
        if (res.IsClosed) return res.IsClosed;

        //Exec global filters
        foreach (var requestFilter in GlobalMessageRequestFiltersArray)
        {
            requestFilter(req, res, requestDto);
            if (res.IsClosed) return res.IsClosed;
        }

        foreach (var requestFilter in GlobalMessageRequestFiltersAsyncArray)
        {
            var task = requestFilter(req, res, requestDto);
            task.Wait();
            if (res.IsClosed) return res.IsClosed;
        }

        return res.IsClosed;
    }

    /// <summary>
    /// Executes MQ Response Filters
    /// </summary>
    public bool ApplyMessageResponseFilters(IRequest req, IResponse res, object response)
    {
        ExecTypedFilters(GlobalTypedMessageResponseFilters, req, res, response);
        if (res.IsClosed) return res.IsClosed;

        //Exec global filters
        foreach (var responseFilter in GlobalMessageResponseFiltersArray)
        {
            responseFilter(req, res, response);
            if (res.IsClosed) return res.IsClosed;
        }

        foreach (var requestFilter in GlobalMessageResponseFiltersAsyncArray)
        {
            requestFilter(req, res, response).Wait();
            if (res.IsClosed) return res.IsClosed;
        }

        return res.IsClosed;
    }

    /// <summary>
    /// Execute Service Gateway Request Filters
    /// </summary>
    public async Task<bool> ApplyGatewayRequestFiltersAsync(IRequest req, object request)
    {
        foreach (var filter in HostContext.AppHost.GatewayRequestFiltersArray)
        {
            filter(req, request);
            if (req.Response.IsClosed)
                return false;
        }
        foreach (var filter in HostContext.AppHost.GatewayRequestFiltersAsyncArray)
        {
            await filter(req, request);
            if (req.Response.IsClosed)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Execute Service Gateway Response Filters
    /// </summary>
    public async Task<bool> ApplyGatewayResponseFiltersAsync(IRequest req, object responseDto)
    {
        foreach (var filter in HostContext.AppHost.GatewayResponseFiltersArray)
        {
            filter(req, responseDto);
            if (req.Response.IsClosed)
                return false;
        }
        foreach (var filter in HostContext.AppHost.GatewayResponseFiltersAsyncArray)
        {
            await filter(req, responseDto);
            if (req.Response.IsClosed)
                return false;
        }
        return true;
    }


    /// <summary>
    /// Executes Typed Request Filters 
    /// </summary>
    public void ExecTypedFilters(Dictionary<Type, ITypedFilter> typedFilters, IRequest req, IResponse res, object dto)
    {
        if (typedFilters.Count == 0) return;

        var dtoType = dto.GetType();
        typedFilters.TryGetValue(dtoType, out var typedFilter);
        if (typedFilter != null)
        {
            typedFilter.Invoke(req, res, dto);
            if (res.IsClosed) return;
        }

        var dtoInterfaces = dtoType.GetInterfaces();
        foreach (var dtoInterface in dtoInterfaces)
        {
            typedFilters.TryGetValue(dtoInterface, out typedFilter);
            if (typedFilter != null)
            {
                typedFilter.Invoke(req, res, dto);
                if (res.IsClosed) return;
            }
        }
    }

    /// <summary>
    /// Executes Typed Request Filters 
    /// </summary>
    public async Task ExecTypedFiltersAsync(Dictionary<Type, ITypedFilterAsync> typedFilters, IRequest req, IResponse res, object dto)
    {
        if (typedFilters.Count == 0) return;

        var dtoType = dto.GetType();
        typedFilters.TryGetValue(dtoType, out var typedFilter);
        if (typedFilter != null)
        {
            await typedFilter.InvokeAsync(req, res, dto);
            if (res.IsClosed) return;
        }

        var dtoInterfaces = dtoType.GetInterfaces();
        foreach (var dtoInterface in dtoInterfaces)
        {
            typedFilters.TryGetValue(dtoInterface, out typedFilter);
            if (typedFilter != null)
            {
                await typedFilter.InvokeAsync(req, res, dto);
                if (res.IsClosed) return;
            }
        }
    }

    public virtual IHttpHandler GetCatchAllHandler(IHttpRequest httpReq)
    {
        foreach (var httpHandlerResolver in CatchAllHandlersArray)
        {
            var httpHandler = httpHandlerResolver(httpReq);
            if (httpHandler != null)
                return httpHandler;
        }

        return null;
    }

    public virtual IHttpHandler GetFallbackHandler(IHttpRequest httpReq)
    {
        // Check for PagedBasedRouting before wildcard Fallback Service
        foreach (var httpHandlerResolver in FallbackHandlersArray)
        {
            var httpHandler = httpHandlerResolver(httpReq);
            if (httpHandler != null)
                return httpHandler;
        }

        if (Config.FallbackRestPath != null)
        {
            var fallbackPath = Config.FallbackRestPath(httpReq);
            if (fallbackPath != null)
            {
                RestHandler.GetSanitizedPathInfo(httpReq.PathInfo, out var contentType);
                return new RestHandler
                {
                    RestPath = fallbackPath, 
                    RequestName = fallbackPath.RequestType.GetOperationName(), 
                    ResponseContentType = contentType,
                };
            }
        }

        return null;
    }

    /// <summary>
    /// Configuration of ServiceStack's /metadata pages
    /// </summary>
    public MetadataPagesConfig MetadataPagesConfig => new(
        Metadata,
        Config.ServiceEndpointsMetadataConfig,
        Config.IgnoreFormatsInMetadata,
        ContentTypes.ContentTypeFormats.Keys.ToList());

    /// <summary>
    /// Return the Default Session Expiry for this Request 
    /// </summary>
    public virtual TimeSpan GetDefaultSessionExpiry(IRequest req)
    {
        var sessionFeature = this.GetPlugin<SessionFeature>();
        if (sessionFeature != null)
        {
            return req.IsPermanentSession()
                ? sessionFeature.PermanentSessionExpiry ?? SessionFeature.DefaultPermanentSessionExpiry
                : sessionFeature.SessionExpiry ?? SessionFeature.DefaultSessionExpiry;
        }

        return req.IsPermanentSession()
            ? SessionFeature.DefaultPermanentSessionExpiry
            : SessionFeature.DefaultSessionExpiry;
    }

    /// <summary>
    /// Return whether this App supports this feature 
    /// </summary>
    public bool HasFeature(Feature feature)
    {
        return (feature & Config.EnableFeatures) == feature;
    }

    /// <summary>
    /// Assert whether this App supports this feature 
    /// </summary>
    public void AssertFeatures(Feature usesFeatures)
    {
        if (Config.EnableFeatures == Feature.All) 
            return;

        if (!HasFeature(usesFeatures))
        {
            throw new UnauthorizedAccessException(
                $"'{usesFeatures}' Features have been disabled by your administrator");
        }
    }

    /// <summary>
    /// Assert whether this App should server this contentType 
    /// </summary>
    public void AssertContentType(string contentType)
    {
        if (Config.EnableFeatures == Feature.All) 
            return;

        AssertFeatures(contentType.ToFeature());
    }

    /// <summary>
    /// Override to return whether this request can access the metadata for the request
    /// </summary>
    public bool HasAccessToMetadata(IRequest httpReq, IResponse httpRes)
    {
        if (!HasFeature(Feature.Metadata))
        {
            HandleErrorResponse(httpReq, httpRes, HttpStatusCode.Forbidden, "Metadata Not Available");
            return false;
        }

        if (Config.MetadataVisibility != RequestAttributes.Any)
        {
            var actualAttributes = httpReq.GetAttributes();
            if ((actualAttributes & Config.MetadataVisibility) != Config.MetadataVisibility)
            {
                HandleErrorResponse(httpReq, httpRes, HttpStatusCode.Forbidden, "Metadata Not Visible");
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Override to handle Forbidden Feature Error Responses
    /// </summary>
    public void HandleErrorResponse(IRequest httpReq, IResponse httpRes, HttpStatusCode errorStatus, string errorStatusDescription = null)
    {
        if (httpRes.IsClosed) return;

        httpRes.StatusDescription = errorStatusDescription;

        var handler = GetCustomErrorHandler(errorStatus)
                      ?? GlobalHtmlErrorHttpHandler
                      ?? GetNotFoundHandler();

        handler.ProcessRequestAsync(httpReq, httpRes, httpReq.OperationName);
    }

    /// <summary>
    /// Override to customize the IServiceStackHandler that should handle the specified errorStatusCode 
    /// </summary>
    public IServiceStackHandler GetCustomErrorHandler(int errorStatusCode)
    {
        try
        {
            return GetCustomErrorHandler((HttpStatusCode)errorStatusCode);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Override to customize the IServiceStackHandler that should handle the specified HttpStatusCode 
    /// </summary>
    public IServiceStackHandler GetCustomErrorHandler(HttpStatusCode errorStatus)
    {
        IServiceStackHandler httpHandler = null;
        CustomErrorHttpHandlers?.TryGetValue(errorStatus, out httpHandler);

        return httpHandler;
    }

    /// <summary>
    /// Override to change the IServiceStackHandler that should handle 404 NotFount Responses
    /// </summary>
    public IServiceStackHandler GetNotFoundHandler()
    {
        IServiceStackHandler httpHandler = null;
        CustomErrorHttpHandlers?.TryGetValue(HttpStatusCode.NotFound, out httpHandler);

        return httpHandler ?? new NotFoundHttpHandler();
    }

    /// <summary>
    /// Override to customize the IHttpHandler that should handle the specified HttpStatusCode 
    /// </summary>
    public IHttpHandler GetCustomErrorHttpHandler(HttpStatusCode errorStatus)
    {
        var ssHandler = GetCustomErrorHandler(errorStatus)
                        ?? GetNotFoundHandler();
        if (ssHandler == null) return null;
        var httpHandler = ssHandler as IHttpHandler;
        return httpHandler ?? new ServiceStackHttpHandler(ssHandler);
    }

    /// <summary>
    /// Return true if the current request is configured with the super user AdminAuthSecret or not  
    /// </summary>
    public bool HasValidAuthSecret(IRequest httpReq)
    {
        if (Config.AdminAuthSecret != null)
        {
            var authSecret = httpReq.GetAuthSecret();
            return authSecret == Config.AdminAuthSecret;
        }

        return false;
    }

    /// <summary>
    /// Override to customize converting an Exception into a generic ErrorResponse DTO  
    /// </summary>
    public virtual ErrorResponse CreateErrorResponse(Exception ex, object request = null) =>
        new() { ResponseStatus = ex.ToResponseStatus() };
        
    /// <summary>
    /// Override to customize converting an Exception into the ResponseStatus DTO  
    /// </summary>
    public virtual ResponseStatus CreateResponseStatus(Exception ex, object request=null)
    {
        var e = UseException(ex.UnwrapIfSingleException());
        var responseStatus = (e is IResponseStatusConvertible customStatus ? customStatus.ToResponseStatus() : null) 
                             ?? ResponseStatusUtils.CreateResponseStatus(e.GetType().Name, e.Message);
        DtoUtils.PopulateResponseStatus(responseStatus, request, ex, Config.DebugMode);
        
        OnExceptionTypeFilter(e, responseStatus);

        if (Config.DebugMode || Log.IsDebugEnabled)
            OnLogError(GetType(), responseStatus.Message, e);
            
        return responseStatus;
    }

    /// <summary>
    /// Return the Exception to use when creating the Error Response DTO and ResponseStatus 
    /// </summary>
    public Exception UseException(Exception ex) =>
        (Config.ReturnsInnerException && ex.InnerException != null && !(ex is IHttpError)
            ? ex.InnerException
            : null) ?? ex;

    /// <summary>
    /// Callback for handling when errors are logged, also called for non-Exception error logging like 404 requests   
    /// </summary>
    public virtual void OnLogError(Type type, string message, Exception innerEx=null)
    {
        if (innerEx != null)
            Log.Error(message, innerEx);
        else
            Log.Error(message);
    }
        
    /// <summary>
    /// Try infer UserId from IRequest
    /// </summary>
    /// <returns></returns>
    public virtual string TryGetUserId(IRequest req)
    {
        var session = req.Items.TryGetValue(Keywords.Session, out var oSession)
            ? oSession as IAuthSession
            : null;
        return session != null 
            ? session.UserAuthId 
            : (req.Cookies.TryGetValue(HttpHeaders.XUserAuthId, out var cUserId) ? cUserId.Value : null) 
              ?? req.Response.GetHeader(HttpHeaders.XUserAuthId);
    }

    /// <summary>
    /// Override to intercept &amp; customize Exception responses 
    /// </summary>
    public virtual void OnExceptionTypeFilter(Exception ex, ResponseStatus responseStatus)
    {
        var argEx = ex as ArgumentException;
        if (argEx?.ParamName != null)
        {
            var paramMsgIndex = argEx.Message.LastIndexOf("Parameter name:", StringComparison.Ordinal);
            if (paramMsgIndex == -1)
                paramMsgIndex = argEx.Message.LastIndexOf("(Parameter", StringComparison.Ordinal); //.NET Core
                
            var errorMsg = paramMsgIndex > 0
                ? argEx.Message.Substring(0, paramMsgIndex).TrimEnd()
                : argEx.Message;

            responseStatus.Errors ??= [];
            responseStatus.Errors.Add(new ResponseError
            {
                ErrorCode = ex.GetType().Name,
                FieldName = argEx.ParamName,
                Message = errorMsg,
            });
        }
        else
        {
            var serializationEx = ex as SerializationException;
            if (serializationEx?.Data["errors"] is List<RequestBindingError> errors)
            {
                responseStatus.Errors ??= [];
                responseStatus.Errors = errors.Select(e => new ResponseError
                {
                    ErrorCode = ex.GetType().Name,
                    FieldName = e.PropertyName,
                    Message = e.PropertyValueString != null 
                        ? $"'{e.PropertyValueString}' is an Invalid value for '{e.PropertyName}'"
                        : $"Invalid Value for '{e.PropertyName}'"
                }).ToList();
            }
        }

        responseStatus.Message = SanitizeString(responseStatus.Message);
        foreach (var error in responseStatus.Errors.Safe())
        {
            error.Message = SanitizeString(error.Message);
        }
    }

    /// <summary>
    /// Sanitize strings against XSS returned in API Responses 
    /// </summary>
    public virtual string SanitizeString(string message)
    {
        if (string.IsNullOrEmpty(message)) return message;
        // guard against XSS https://stackoverflow.com/q/78741326/85785
        if (message.IndexOf("<script", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return message.HtmlEncodeLite();
        }
        return message;
    }
    
    /// <summary>
    /// Override to intercept when Sessions using sync APIs are saved
    /// </summary>
    [Obsolete("Use OnSaveSessionAsync")]
    public virtual void OnSaveSession(IRequest httpReq, IAuthSession session, TimeSpan? expiresIn = null)
    {
        if (httpReq == null) return;
                        
        if (session.FromToken) // Don't persist Sessions populated from tokens 
            return; 

        session.LastModified = DateTime.UtcNow;
        httpReq.Items[Keywords.Session] = session;
        if (!HasPlugin<SessionFeature>())
            return;

        var sessionKey = SessionFeature.GetSessionKey(session.Id ?? httpReq.GetOrCreateSessionId());
        this.GetCacheClient(httpReq).CacheSet(sessionKey, session, expiresIn ?? GetDefaultSessionExpiry(httpReq));
    }

    /// <summary>
    /// Override to intercept when Sessions using async APIs are saved
    /// </summary>
    public virtual Task OnSaveSessionAsync(IRequest httpReq, IAuthSession session, TimeSpan? expiresIn = null, CancellationToken token=default)
    {
        if (httpReq == null || session.FromToken) // Don't persist Sessions populated from tokens 
            return TypeConstants.EmptyTask;

        session.LastModified = DateTime.UtcNow;
        httpReq.Items[Keywords.Session] = session;
        if (!HasPlugin<SessionFeature>())
            return Task.CompletedTask;
        
        var sessionKey = SessionFeature.GetSessionKey(session.Id ?? httpReq.GetOrCreateSessionId());
        return this.GetCacheClientAsync(httpReq).CacheSetAsync(sessionKey, session, expiresIn ?? GetDefaultSessionExpiry(httpReq), token);
    }

    /// <summary>
    /// Inspect or modify ever new UserSession created or resolved from cache. 
    /// return null if Session is invalid to create new Session.
    /// </summary>
    public virtual IAuthSession OnSessionFilter(IRequest req, IAuthSession session, string withSessionId)
    {
        if (session is IAuthSessionExtended authSession)
        {
            authSession.OnLoad(req);
        }
        return session;
    }

#if !NETFRAMEWORK
    /// <summary>
    /// Modify Cookie options
    /// </summary>
    public virtual void CookieOptionsFilter(Cookie cookie, Microsoft.AspNetCore.Http.CookieOptions cookieOptions) {}
#else
        public virtual void HttpCookieFilter(HttpCookie cookie) {}
#endif
        
    /// <summary>
    /// Override built-in Cookies, return false to prevent the Cookie from being set.
    /// </summary>
    public virtual bool SetCookieFilter(IRequest req, Cookie cookie)
    {
        if (req.Response.HasStarted)
            return false;
#pragma warning disable CS0618
        return AllowSetCookie(req, cookie.Name);
#pragma warning restore CS0618
    }
        
    [Obsolete("Override SetCookieFilter")]
    protected virtual bool AllowSetCookie(IRequest req, string cookieName)
    {
        if (!Config.AllowSessionCookies)
            return cookieName != SessionFeature.SessionId
                   && cookieName != SessionFeature.PermanentSessionId
                   && cookieName != SessionFeature.SessionOptionsKey
                   && cookieName != SessionFeature.XUserAuthId;

        return true;
    }

    /// <summary>
    /// Overriden by AppHost's to return the current IRequest if it supports singleton access to the Request Context 
    /// </summary>
    public virtual IRequest TryGetCurrentRequest()
    {
        return null;
    }

    /// <summary>
    /// Override to intercept the response of a ServiceStack Service request
    /// </summary>
    public virtual object OnAfterExecute(IRequest req, object requestDto, object response)
    {
        req.Response.Dto ??= response;
        return response;
    }

    public virtual void OnLogRequest(IRequest req, object requestDto, object response, TimeSpan elapsed)
    {
        GetPlugin<RequestLogsFeature>()?.RequestLogger.Log(req, requestDto, response, elapsed);
#if NET6_0_OR_GREATER
        if (requestDto != null)
        {
            GetPlugin<CommandsFeature>()?.AddRequest(requestDto, response, elapsed);
        }
#endif
    }

    /// <summary>
    /// Override to customize what DTOs are displayed on metadata pages
    /// </summary>
    public virtual MetadataTypesConfig GetTypesConfigForMetadata(IRequest req)
    {
        var typesConfig = new NativeTypesFeature().MetadataTypesConfig;
        typesConfig.IgnoreTypesInNamespaces.Clear();
        typesConfig.IgnoreTypes.Add(typeof(ResponseStatus));
        typesConfig.IgnoreTypes.Add(typeof(ResponseError));
        return typesConfig;
    }

    /// <summary>
    /// Gets IDbConnection Checks if DbInfo is seat in RequestContext.
    /// See multitenancy: https://docs.servicestack.net/multitenancy
    /// Called by itself, <see cref="Service"></see> and ViewPageBase
    /// </summary>
    /// <param name="req">Provided by services and pageView, can be helpful when overriding this method</param>
    /// <returns></returns>
    public virtual IDbConnection GetDbConnection(IRequest req = null)
    {
        var dbFactory = Container.TryResolve<IDbConnectionFactory>();
        if (req != null)
        {
            if (req.GetItem(Keywords.DbInfo) is ConnectionInfo connInfo)
            {
                if (dbFactory is not IDbConnectionFactoryExtended dbFactoryExtended)
                    throw new NotSupportedException("ConnectionInfo can only be used with IDbConnectionFactoryExtended");

                if (connInfo.ConnectionString != null)
                {
                    return connInfo.ProviderName != null 
                        ? dbFactoryExtended.OpenDbConnectionString(connInfo.ConnectionString, connInfo.ProviderName) 
                        : dbFactoryExtended.OpenDbConnectionString(connInfo.ConnectionString);
                }

                if (connInfo.NamedConnection != null)
                    return dbFactoryExtended.OpenDbConnection(connInfo.NamedConnection);
            }
            else
            {
                var namedConnectionAttr = req.Dto?.GetType().FirstAttribute<NamedConnectionAttribute>();
                if (namedConnectionAttr != null)
                {
                    if (dbFactory is not IDbConnectionFactoryExtended dbFactoryExtended)
                        throw new NotSupportedException("ConnectionInfo can only be used with IDbConnectionFactoryExtended");

                    return dbFactoryExtended.OpenDbConnection(namedConnectionAttr.Name);
                }
            }
        }

        return dbFactory.OpenDbConnection();
    }

    public virtual IDbConnection GetDbConnection(string namedConnection)
    {
        var dbFactory = Container.TryResolve<IDbConnectionFactory>();

        if (namedConnection == null) 
            return dbFactory.OpenDbConnection();
            
        if (dbFactory is not IDbConnectionFactoryExtended dbFactoryExtended)
            throw new NotSupportedException("ConnectionInfo can only be used with IDbConnectionFactoryExtended");
            
        return dbFactoryExtended.OpenDbConnection(namedConnection);
    }

    public virtual string GetDbNamedConnection(IRequest req)
    {
        if (req.GetItem(Keywords.DbInfo) is ConnectionInfo connInfo)
            return connInfo.NamedConnection;
        var namedConnectionAttr = req.Dto?.GetType().FirstAttribute<NamedConnectionAttribute>();
        return namedConnectionAttr?.Name;
    }

    /// <summary>
    /// Resolves <see cref="IRedisClient"></see> based on <see cref="IRedisClientsManager"></see>.GetClient();
    /// Called by itself, <see cref="Service"></see> and ViewPageBase
    /// </summary>
    /// <param name="req">Provided by services and pageView, can be helpful when overriding this method</param>
    /// <returns></returns>
    public virtual IRedisClient GetRedisClient(IRequest req = null)
    {
        return Container.TryResolve<IRedisClientsManager>()?.GetClient();
    }

    /// <summary>
    /// Resolves <see cref="IRedisClient"></see> based on <see cref="IRedisClientsManager"></see>.GetClient();
    /// Called by itself, <see cref="Service"></see> and ViewPageBase
    /// </summary>
    /// <param name="req">Provided by services and pageView, can be helpful when overriding this method</param>
    /// <returns></returns>
    public virtual ValueTask<IRedisClientAsync> GetRedisClientAsync(IRequest req = null)
    {
        var asyncManager = Container.TryResolve<IRedisClientsManagerAsync>();
        if (asyncManager != null)
            return asyncManager.GetClientAsync();

        var manager = Container.TryResolve<IRedisClientsManager>();
        if (manager is IRedisClientsManagerAsync managerAsync)
            return managerAsync.GetClientAsync();

        return default;
    }

    /// <summary>
    /// If they don't have an ICacheClient configured use an In Memory one.
    /// </summary>
    public static MemoryCacheClient DefaultCache { get; } = new();

    /// <summary>
    /// Tries to resolve <see cref="ICacheClient"></see> through IoC container.
    /// If not registered, it falls back to <see cref="IRedisClientsManager"></see>.GetClient(),
    /// otherwise returns DefaultCache MemoryCacheClient
    /// </summary>
    public virtual ICacheClient GetCacheClient(IRequest req = null)
    {
        var resolver = req ?? (IResolver) this;
        var cache = resolver.TryResolve<ICacheClient>();
        if (cache != null)
            return cache;

        var redisManager = resolver.TryResolve<IRedisClientsManager>();
        if (redisManager != null)
            return redisManager.GetCacheClient();

        return DefaultCache;
    }

    /// <summary>
    /// Get registered ICacheClientAsync otherwise returns async wrapped sync ICacheClient
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    public virtual ICacheClientAsync GetCacheClientAsync(IRequest req = null) => 
        (req ?? (IResolver) this).TryResolve<ICacheClientAsync>() ?? GetCacheClient(req).AsAsync();

    /// <summary>
    /// Only sets cacheAsync if native Async provider, otherwise sets cacheSync
    /// </summary>
    public virtual void TryGetNativeCacheClient(IRequest req, out ICacheClient cacheSync, out ICacheClientAsync cacheAsync)
    {
        cacheSync = GetCacheClient(req);
        cacheAsync = (req ?? (IResolver)this).TryResolve<ICacheClientAsync>();
        if (cacheAsync.Unwrap() != null) // non-null if wraps sync ICacheClient
            cacheAsync = null;
        else if (cacheAsync != null)
            cacheSync = null;
    }
        
    /// <summary>
    /// Returns <see cref="MemoryCacheClient"></see>. cache is only persisted for this running app instance.
    /// Called by <see cref="Service"></see>.MemoryCacheClient
    /// </summary>
    /// <param name="req">Provided by services and pageView, can be helpful when overriding this method</param>
    /// <returns>Nullable MemoryCacheClient</returns>
    public virtual MemoryCacheClient GetMemoryCacheClient(IRequest req=null) => Container.TryResolve<MemoryCacheClient>() ?? DefaultCache;

    /// <summary>
    /// Returns <see cref="IMessageProducer"></see> from the IOC container.
    /// Called by itself, <see cref="Service"></see> and ViewPageBase
    /// </summary>
    /// <param name="req">Provided by services and PageViewBase, can be helpful when overriding this method</param>
    /// <returns></returns>
    public virtual IMessageProducer GetMessageProducer(IRequest req = null)
    {
        return (Container.TryResolve<IMessageFactory>()
                ?? Container.TryResolve<IMessageService>().MessageFactory).CreateMessageProducer();
    }

    /// <summary>
    /// Get the configured <see cref="IServiceGateway"/>  
    /// </summary>
    public virtual IServiceGateway GetServiceGateway() => GetServiceGateway(new GatewayRequest());

    /// <summary>
    /// Get the configured <see cref="IServiceGateway"/> for this request.  
    /// </summary>
    public virtual IServiceGateway GetServiceGateway(IRequest req)
    {
        if (req == null)
            throw new ArgumentNullException(nameof(req));

        var factory = Container.TryResolve<IServiceGatewayFactory>();
        var ret = factory != null ? factory.GetServiceGateway(req) 
            : Container.TryResolve<IServiceGateway>()
              ?? new InProcessServiceGateway(GatewayRequest.Create(req));
        return ret;
    }

    /// <summary>
    /// Gets the registered <see cref="IAuthRepository"/>  
    /// </summary>
    public virtual IAuthRepository GetAuthRepository(IRequest req = null)
    {
        return TryResolve<IAuthRepository>();
    }

    /// <summary>
    /// Gets the registered <see cref="IAuthRepositoryAsync"/>
    /// Returns native IAuthRepositoryAsync if exists, a sync wrapper if IAuthRepository exists, otherwise null.
    /// </summary>
    public virtual IAuthRepositoryAsync GetAuthRepositoryAsync(IRequest req = null)
    {
        var authRepoAsync = TryResolve<IAuthRepositoryAsync>();
        if (authRepoAsync != null)
            return authRepoAsync;

        var authRepo = GetAuthRepository(req);
        return authRepo.AsAsync();
    }

    /// <summary>
    /// Default logic to determine whether a Session is Authenticated
    /// </summary>
    public virtual IAuthSession AssertAuthenticated(IAuthSession session, IRequest req=null)
    {
        if (session?.IsAuthenticated != true)
            throw new HttpError(HttpStatusCode.Unauthorized, ErrorMessages.NotAuthenticated.Localize(req));

        return session;
    }

    /// <summary>
    /// Return the ICookies implementation to use
    /// </summary>
    public virtual ICookies GetCookies(IHttpResponse res) => new Cookies(res);

    /// <summary>
    /// Override to return whether static files should be sent compressed or not (if supported by UserAgent)
    /// </summary>
    public virtual bool ShouldCompressFile(IVirtualFile file)
    {
        return !string.IsNullOrEmpty(file.Extension) 
               && Config.CompressFilesWithExtensions.Contains(file.Extension)
               && (Config.CompressFilesLargerThanBytes == null || file.Length > Config.CompressFilesLargerThanBytes);
    }

    /// <summary>
    /// Allow overriding ServiceStack runtime config like JWT Keys
    /// </summary>
    public virtual T GetRuntimeConfig<T>(IRequest req, string name, T defaultValue)
    {
        var runtimeAppSettings = TryResolve<IRuntimeAppSettings>();
        if (runtimeAppSettings != null)
            return runtimeAppSettings.Get(req, name, defaultValue);

        return defaultValue;
    }

    /// <summary>
    /// Override to intercept MQ Publish Requests 
    /// </summary>
    public virtual void PublishMessage<T>(IMessageProducer messageProducer, T message)
    {
        if (messageProducer == null)
            throw new ArgumentNullException(nameof(messageProducer), @"No IMessageFactory was registered, cannot PublishMessage");
            
        messageProducer.Publish(message);
    }

    /// <summary>
    /// Override to intercept auto HTML Page Response
    /// </summary>
    public virtual async Task WriteAutoHtmlResponseAsync(IRequest req, object response, string html, Stream outputStream)
    {
        if (!Config.EnableAutoHtmlResponses)
        {
            req.ResponseContentType = Config.DefaultContentType
                                      ?? Config.PreferredContentTypesArray[0];

            if (req.ResponseContentType.MatchesContentType(MimeTypes.Html))
                req.ResponseContentType = Config.PreferredContentTypesArray.First(x => !x.MatchesContentType(MimeTypes.Html));

            await req.Response.WriteToResponse(req, response).ConfigAwait();
            return;
        }

        var utf8Bytes = html.ToUtf8Bytes();
        await outputStream.WriteAsync(utf8Bytes, 0, utf8Bytes.Length).ConfigAwait();
    }

    /// <summary>
    /// Override to alter what registered plugins you want discoverable (used by ServiceStack Studio to enable features). 
    /// </summary>
    public virtual List<string> GetMetadataPluginIds()
    {
        var pluginIds = Plugins.OfType<IHasStringId>().Map(x => x.Id);
        return pluginIds;
    }

    /// <summary>
    /// Override Authorization Header resolution
    /// </summary>
    public virtual string GetAuthorization(IRequest req)
    {
        var auth = req.Items.TryGetValue(Keywords.Authorization, out var oAuth)
            ? oAuth as string
            : null;
        if (!string.IsNullOrEmpty(auth))
            return auth;
            
        auth = req.Authorization;
        return string.IsNullOrEmpty(auth) ? null : auth;
    }

    /// <summary>
    /// Override Authorization Bearer Token resolution
    /// </summary>
    public virtual string GetBearerToken(IRequest req)
    {
        if (req.Dto is IHasBearerToken { BearerToken: { } } dto)
            return dto.BearerToken;
            
        var auth = GetAuthorization(req);
        if (string.IsNullOrEmpty(auth))
            return null;

        var pos = auth.IndexOf(' ');
        if (pos < 0)
            return null;

        var ret = auth.StartsWith("Bearer", StringComparison.OrdinalIgnoreCase) 
            ? auth.Substring(pos + 1) 
            : null;
        if (!string.IsNullOrEmpty(ret))
            return ret;
        return null;            
    }

    /// <summary>
    /// Override JWT Token resolution
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    public virtual string GetJwtToken(IRequest req)
    {
        var jwtAuthProvider = AuthenticateService.GetJwtAuthProvider();
        if (jwtAuthProvider != null)
        {
            if (jwtAuthProvider.AllowInFormData)
            {
                var jwt = req.FormData[Keywords.TokenCookie];
                if (!string.IsNullOrEmpty(jwt))
                    return jwt;
            }

            if (jwtAuthProvider.AllowInQueryString)
            {
                var jwt = req.QueryString[Keywords.TokenCookie];
                if (!string.IsNullOrEmpty(jwt))
                    return jwt;
            }
        }

        return GetBearerToken(req) ??
               req.GetCookieValue(Keywords.TokenCookie);
    }

    /// <summary>
    /// Override Authorization Refresh Token resolution
    /// </summary>
    public virtual string GetRefreshToken(IRequest req)
    {
        if (req.Dto is IHasRefreshToken { RefreshToken: not null } dto)
            return dto.RefreshToken;
        return null;            
    }

    /// <summary>
    /// Override JWT Refresh Token resolution
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    public virtual string GetJwtRefreshToken(IRequest req)
    {
        var jwtAuthProvider = AuthenticateService.GetJwtAuthProvider();
        if (jwtAuthProvider != null)
        {
            if (jwtAuthProvider.AllowInFormData)
            {
                var jwt = req.FormData[Keywords.RefreshTokenCookie];
                if (!string.IsNullOrEmpty(jwt))
                    return jwt;
            }

            if (jwtAuthProvider.AllowInQueryString)
            {
                var jwt = req.QueryString[Keywords.RefreshTokenCookie];
                if (!string.IsNullOrEmpty(jwt))
                    return jwt;
            }
        }

        return GetRefreshToken(req) ??
               req.GetCookieValue(Keywords.RefreshTokenCookie);
    }

    public virtual IHttpResult Redirect(IServiceBase service, string redirect, string message)
    {
        return new HttpResult(HttpStatusCode.Redirect, message)
        {
            ContentType = service.Request.ResponseContentType,
            Headers = {
                { HttpHeaders.Location, redirect }
            },
        };
    }

    public virtual IHttpResult LocalRedirect(IServiceBase service, string redirect, string message)
    {
        redirect = redirect?.Trim();
        if (!string.IsNullOrEmpty(redirect))
        {
            if (!redirect.StartsWith("//") && redirect.IndexOf("://", StringComparison.Ordinal) < 0)
                return service.Redirect(redirect, message);

            if (redirect.StartsWith(service.Request.GetBaseUrl()))
                return service.Redirect(redirect, message);
        }
        return service.Redirect(service.Request.GetBaseUrl(), message);
    }

    public virtual IHttpResult AuthenticationRequired(IServiceBase service)
    {
        return new HttpResult
        {
            StatusCode = HttpStatusCode.Unauthorized,
            ContentType = service.Request.ResponseContentType,
            Headers = {
                { HttpHeaders.WwwAuthenticate, $"{AuthenticateService.DefaultOAuthProvider} realm=\"{AuthenticateService.DefaultOAuthRealm}\"" }
            },
        };
    }

    public virtual void OnSerializeJson(IRequest req, object dto, Stream outputStream) =>
        JsonDataContractSerializer.Instance.SerializeToStream(dto, outputStream);

    public virtual object OnDeserializeJson(Type intoType, Stream fromStream) =>
        JsonDataContractSerializer.Instance.DeserializeFromStream(intoType, fromStream);

 
    public virtual string GetCompressionType(IRequest request)
    {
        var isFireFox = request.UserAgent?.IndexOf("firefox", StringComparison.OrdinalIgnoreCase) > 0; 
        if (!isFireFox)
        {
            //CompressionTypes.Brotli;
            if (request.RequestPreferences.AcceptsBrotli && StreamCompressors.SupportsEncoding("br"))
                return "br";
        }
            
        if (request.RequestPreferences.AcceptsDeflate && StreamCompressors.SupportsEncoding(CompressionTypes.Deflate))
            return CompressionTypes.Deflate;

        if (request.RequestPreferences.AcceptsGzip && StreamCompressors.SupportsEncoding(CompressionTypes.GZip))
            return CompressionTypes.GZip;

        return null;
    }

    public virtual string CreateSessionId() => SessionExtensions.CreateRandomSessionId();

    public virtual bool HasUi() => HasPlugin<UiFeature>();

    public virtual bool ShouldProfileRequest(IRequest req) =>
        Diagnostics.ServiceStack.IsEnabled(Diagnostics.Events.ServiceStack.WriteRequestBefore)
        && HasPlugin<ProfilingFeature>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void OnAfterAwait(IRequest req,
        [System.Runtime.CompilerServices.CallerFilePath] string file = "",
        [System.Runtime.CompilerServices.CallerLineNumber] long line = 0,
        [System.Runtime.CompilerServices.CallerMemberName] string member = "")
    {
#if NETFRAMEWORK
            if (!Log.IsDebugEnabled) return;
            if (HttpContext.Current == null && req is ServiceStack.Host.AspNet.AspNetRequest aspReq)
            {
                Log.DebugFormat("HttpContext.Current == null at {0} in {1}:line {2}",
                    member, file, line);
            }
#endif
    }
}