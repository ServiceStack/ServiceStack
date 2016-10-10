// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Web;
using System.Xml;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.FluentValidation;
using ServiceStack.Host;
using ServiceStack.Host.Handlers;
using ServiceStack.Messaging;
using ServiceStack.Metadata;
using ServiceStack.MiniProfiler;
using ServiceStack.Redis;
using ServiceStack.Serialization;
using ServiceStack.Support.WebHost;
using ServiceStack.Web;

namespace ServiceStack
{
    public abstract partial class ServiceStackHost
    {
        public virtual object ApplyRequestConverters(IRequest req, object requestDto)
        {
            foreach (var converter in RequestConverters)
            {
                requestDto = converter(req, requestDto) ?? requestDto;
                if (req.Response.IsClosed)
                    return requestDto;
            }

            return requestDto;
        }

        public virtual object ApplyResponseConverters(IRequest req, object responseDto)
        {
            foreach (var converter in ResponseConverters)
            {
                responseDto = converter(req, responseDto) ?? responseDto;
                if (req.Response.IsClosed)
                    return responseDto;
            }

            return responseDto;
        }

        /// <summary>
        /// Apply PreRequest Filters for participating Custom Handlers, e.g. RazorFormat, MarkdownFormat, etc
        /// </summary>
        public virtual bool ApplyCustomHandlerRequestFilters(IRequest httpReq, IResponse httpRes)
        {
            return ApplyPreRequestFilters(httpReq, httpRes);
        }

        /// <summary>
        /// Apply PreAuthenticate Filters from IAuthWithRequest AuthProviders
        /// </summary>
        public virtual void ApplyPreAuthenticateFilters(IRequest httpReq, IResponse httpRes)
        {
            httpReq.Items[Keywords.HasPreAuthenticated] = true;
            foreach (var authProvider in AuthenticateService.AuthWithRequestProviders)
            {
                authProvider.PreAuthenticate(httpReq, httpRes);
            }
        }

        /// <summary>
        /// Applies the raw request filters. Returns whether or not the request has been handled 
        /// and no more processing should be done.
        /// </summary>
        /// <returns></returns>
        public virtual bool ApplyPreRequestFilters(IRequest httpReq, IResponse httpRes)
        {
            if (PreRequestFilters.Count == 0)
                return false;

            using (Profiler.Current.Step("Executing Pre RequestFilters"))
            {
                foreach (var requestFilter in PreRequestFilters)
                {
                    requestFilter(httpReq, httpRes);
                    if (httpRes.IsClosed) break;
                }

                return httpRes.IsClosed;
            }
        }

        /// <summary>
        /// Applies the request filters. Returns whether or not the request has been handled 
        /// and no more processing should be done.
        /// </summary>
        /// <returns></returns>
        public virtual bool ApplyRequestFilters(IRequest req, IResponse res, object requestDto)
        {
            req.ThrowIfNull("req");
            res.ThrowIfNull("res");

            if (res.IsClosed)
                return true;

            using (Profiler.Current.Step("Executing Request Filters"))
            {
                if (!req.IsMultiRequest())
                    return ApplyRequestFiltersSingle(req, res, requestDto);

                var dtos = (IEnumerable)requestDto;
                foreach (var dto in dtos)
                {
                    if (ApplyRequestFiltersSingle(req, res, dto))
                        return true;
                }
                return false;
            }
        }

        protected virtual bool ApplyRequestFiltersSingle(IRequest req, IResponse res, object requestDto)
        {
            //Exec all RequestFilter attributes with Priority < 0
            var attributes = FilterAttributeCache.GetRequestFilterAttributes(requestDto.GetType());
            var i = 0;
            for (; i < attributes.Length && attributes[i].Priority < 0; i++)
            {
                var attribute = attributes[i];
                Container.AutoWire(attribute);
                attribute.RequestFilter(req, res, requestDto);
                Release(attribute);
                if (res.IsClosed) return res.IsClosed;
            }

            ExecTypedFilters(GlobalTypedRequestFilters, req, res, requestDto);
            if (res.IsClosed) return res.IsClosed;

            //Exec global filters
            foreach (var requestFilter in GlobalRequestFilters)
            {
                requestFilter(req, res, requestDto);
                if (res.IsClosed) return res.IsClosed;
            }

            //Exec remaining RequestFilter attributes with Priority >= 0
            for (; i < attributes.Length && attributes[i].Priority >= 0; i++)
            {
                var attribute = attributes[i];
                Container.AutoWire(attribute);
                attribute.RequestFilter(req, res, requestDto);
                Release(attribute);
                if (res.IsClosed) return res.IsClosed;
            }

            return res.IsClosed;
        }

        /// <summary>
        /// Applies the response filters. Returns whether or not the request has been handled 
        /// and no more processing should be done.
        /// </summary>
        /// <returns></returns>
        public virtual bool ApplyResponseFilters(IRequest req, IResponse res, object response)
        {
            req.ThrowIfNull("req");
            res.ThrowIfNull("res");

            if (res.IsClosed)
                return true;
            using (Profiler.Current.Step("Executing Response Filters"))
            {
                var batchResponse = req.IsMultiRequest() ? response as IEnumerable : null;
                if (batchResponse == null)
                    return ApplyResponseFiltersSingle(req, res, response);

                foreach (var dto in batchResponse)
                {
                    if (ApplyResponseFiltersSingle(req, res, dto))
                        return true;
                }
                return false;
            }
        }

        protected virtual bool ApplyResponseFiltersSingle(IRequest req, IResponse res, object response)
        {
            var responseDto = response.GetResponseDto();
            var attributes = responseDto != null
                ? FilterAttributeCache.GetResponseFilterAttributes(responseDto.GetType())
                : null;

            //Exec all ResponseFilter attributes with Priority < 0
            var i = 0;
            if (attributes != null)
            {
                for (; i < attributes.Length && attributes[i].Priority < 0; i++)
                {
                    var attribute = attributes[i];
                    Container.AutoWire(attribute);
                    attribute.ResponseFilter(req, res, response);
                    Release(attribute);
                    if (res.IsClosed) return res.IsClosed;
                }
            }

            if (response != null)
            {
                ExecTypedFilters(GlobalTypedResponseFilters, req, res, response);
                if (res.IsClosed) return res.IsClosed;
            }

            //Exec global filters
            foreach (var responseFilter in GlobalResponseFilters)
            {
                responseFilter(req, res, response);
                if (res.IsClosed) return res.IsClosed;
            }

            //Exec remaining RequestFilter attributes with Priority >= 0
            if (attributes != null)
            {
                for (; i < attributes.Length; i++)
                {
                    var attribute = attributes[i];
                    Container.AutoWire(attribute);
                    attribute.ResponseFilter(req, res, response);
                    Release(attribute);
                    if (res.IsClosed) return res.IsClosed;
                }
            }

            return res.IsClosed;
        }

        public virtual bool ApplyMessageRequestFilters(IRequest req, IResponse res, object requestDto)
        {
            ExecTypedFilters(GlobalTypedMessageRequestFilters, req, res, requestDto);
            if (res.IsClosed) return res.IsClosed;

            //Exec global filters
            foreach (var requestFilter in GlobalMessageRequestFilters)
            {
                requestFilter(req, res, requestDto);
                if (res.IsClosed) return res.IsClosed;
            }

            return res.IsClosed;
        }

        public virtual bool ApplyMessageResponseFilters(IRequest req, IResponse res, object response)
        {
            ExecTypedFilters(GlobalTypedMessageResponseFilters, req, res, response);
            if (res.IsClosed) return res.IsClosed;

            //Exec global filters
            foreach (var responseFilter in GlobalMessageResponseFilters)
            {
                responseFilter(req, res, response);
                if (res.IsClosed) return res.IsClosed;
            }

            return res.IsClosed;
        }

        public virtual void ExecTypedFilters(Dictionary<Type, ITypedFilter> typedFilters,
            IRequest req, IResponse res, object dto)
        {
            if (typedFilters.Count == 0) return;

            ITypedFilter typedFilter;
            var dtoType = dto.GetType();
            typedFilters.TryGetValue(dtoType, out typedFilter);
            if (typedFilter != null)
            {
                typedFilter.Invoke(req, res, dto);
                if (res.IsClosed) return;
            }

            var dtoInterfaces = dtoType.GetTypeInterfaces();
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

        public MetadataPagesConfig MetadataPagesConfig => new MetadataPagesConfig(
            Metadata,
            Config.ServiceEndpointsMetadataConfig,
            Config.IgnoreFormatsInMetadata,
            ContentTypes.ContentTypeFormats.Keys.ToList());

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

        public bool HasFeature(Feature feature)
        {
            return (feature & Config.EnableFeatures) == feature;
        }

        public void AssertFeatures(Feature usesFeatures)
        {
            if (Config.EnableFeatures == Feature.All) return;

            if (!HasFeature(usesFeatures))
            {
                throw new UnauthorizedAccessException(
                    $"'{usesFeatures}' Features have been disabled by your administrator");
            }
        }

        public void AssertContentType(string contentType)
        {
            if (Config.EnableFeatures == Feature.All) return;

            AssertFeatures(contentType.ToFeature());
        }

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

        public void HandleErrorResponse(IRequest httpReq, IResponse httpRes, HttpStatusCode errorStatus, string errorStatusDescription = null)
        {
            if (httpRes.IsClosed) return;

            httpRes.StatusDescription = errorStatusDescription;

            var handler = GetCustomErrorHandler(errorStatus)
                ?? GlobalHtmlErrorHttpHandler
                ?? GetNotFoundHandler();

            handler.ProcessRequest(httpReq, httpRes, httpReq.OperationName);
        }

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

        public IServiceStackHandler GetCustomErrorHandler(HttpStatusCode errorStatus)
        {
            IServiceStackHandler httpHandler = null;
            if (CustomErrorHttpHandlers != null)
            {
                CustomErrorHttpHandlers.TryGetValue(errorStatus, out httpHandler);
            }

            return httpHandler;
        }

        public IServiceStackHandler GetNotFoundHandler()
        {
            IServiceStackHandler httpHandler = null;
            if (CustomErrorHttpHandlers != null)
            {
                CustomErrorHttpHandlers.TryGetValue(HttpStatusCode.NotFound, out httpHandler);
            }

            return httpHandler ?? new NotFoundHttpHandler();
        }

        public IHttpHandler GetCustomErrorHttpHandler(HttpStatusCode errorStatus)
        {
            var ssHandler = GetCustomErrorHandler(errorStatus)
                ?? GetNotFoundHandler();
            if (ssHandler == null) return null;
            var httpHandler = ssHandler as IHttpHandler;
            return httpHandler ?? new ServiceStackHttpHandler(ssHandler);
        }

        public bool HasValidAuthSecret(IRequest httpReq)
        {
            if (Config.AdminAuthSecret != null)
            {
                var authSecret = httpReq.GetParam(Keywords.AuthSecret);
                return authSecret == Config.AdminAuthSecret;
            }

            return false;
        }

        public virtual Exception ResolveResponseException(Exception ex)
        {
            return Config.ReturnsInnerException && ex.InnerException != null && !(ex is IHttpError)
                ? ex.InnerException
                : ex;
        }

        public virtual void OnExceptionTypeFilter(Exception ex, ResponseStatus responseStatus)
        {
            var argEx = ex as ArgumentException;
            var isValidationSummaryEx = argEx is ValidationException;
            if (argEx != null && !isValidationSummaryEx && argEx.ParamName != null)
            {
                var paramMsgIndex = argEx.Message.LastIndexOf("Parameter name:", StringComparison.Ordinal);
                var errorMsg = paramMsgIndex > 0
                    ? argEx.Message.Substring(0, paramMsgIndex)
                    : argEx.Message;

                if (responseStatus.Errors == null)
                    responseStatus.Errors = new List<ResponseError>();

                responseStatus.Errors.Add(new ResponseError
                {
                    ErrorCode = ex.GetType().Name,
                    FieldName = argEx.ParamName,
                    Message = errorMsg,
                });
                return;
            }

            var serializationEx = ex as SerializationException;
            var errors = serializationEx?.Data["errors"] as List<RequestBindingError>;
            if (errors != null)
            {
                if (responseStatus.Errors == null)
                    responseStatus.Errors = new List<ResponseError>();

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

        public virtual void OnLogError(Type type, string message, Exception innerEx=null)
        {
            if (innerEx != null)
                Log.Error(message, innerEx);
            else
                Log.Error(message);
        }

        public virtual void OnSaveSession(IRequest httpReq, IAuthSession session, TimeSpan? expiresIn = null)
        {
            if (httpReq == null) return;

            var sessionKey = SessionFeature.GetSessionKey(session.Id ?? httpReq.GetOrCreateSessionId());
            session.LastModified = DateTime.UtcNow;
            this.GetCacheClient().CacheSet(sessionKey, session, expiresIn ?? GetDefaultSessionExpiry(httpReq));

            httpReq.Items[Keywords.Session] = session;
        }

        /// <summary>
        /// Inspect or modify ever new UserSession created or resolved from cache. 
        /// return null if Session is invalid to create new Session.
        /// </summary>
        public virtual IAuthSession OnSessionFilter(IAuthSession session, string withSessionId)
        {
            if (session == null || !SessionFeature.VerifyCachedSessionId)
                return session;

            if (session.Id == withSessionId)
                return session;

            if (Log.IsDebugEnabled)
            {
                Log.Debug($"ignoring cached sessionId '{session.Id}' which is different to request '{withSessionId}'");
            }
            return null;
        }

        public virtual bool AllowSetCookie(IRequest req, string cookieName)
        {
            if (!Config.AllowSessionCookies)
                return cookieName != SessionFeature.SessionId
                    && cookieName != SessionFeature.PermanentSessionId
                    && cookieName != SessionFeature.SessionOptionsKey
                    && cookieName != SessionFeature.XUserAuthId;

            return true;
        }

        public virtual IRequest TryGetCurrentRequest()
        {
            return null;
        }

        public virtual object OnAfterExecute(IRequest req, object requestDto, object response)
        {
            if (req.Response.Dto == null)
                req.Response.Dto = response;

            return response;
        }

        public virtual MetadataTypesConfig GetTypesConfigForMetadata(IRequest req)
        {
            var typesConfig = new NativeTypesFeature().MetadataTypesConfig;
            typesConfig.IgnoreTypesInNamespaces.Clear();
            typesConfig.IgnoreTypes.Add(typeof(ResponseStatus));
            typesConfig.IgnoreTypes.Add(typeof(ResponseError));
            return typesConfig;
        }

        public virtual List<Type> ExportSoapOperationTypes(List<Type> operationTypes)
        {
            var types = operationTypes
                .Where(x => !x.AllAttributes<ExcludeAttribute>()
                            .Any(attr => attr.Feature.Has(Feature.Soap)))
                .Where(x => !x.IsGenericTypeDefinition())
                .ToList();
            return types;
        }

        public virtual bool ExportSoapType(Type type)
        {
            return !type.IsGenericTypeDefinition() &&
                   !type.AllAttributes<ExcludeAttribute>()
                        .Any(attr => attr.Feature.Has(Feature.Soap));
        }

        public virtual IDbConnection GetDbConnection(IRequest req = null)
        {
            var dbFactory = Container.TryResolve<IDbConnectionFactory>();

            ConnectionInfo connInfo;
            if (req != null && (connInfo = req.GetItem(Keywords.DbInfo) as ConnectionInfo) != null)
            {
                var dbFactoryExtended = dbFactory as IDbConnectionFactoryExtended;
                if (dbFactoryExtended == null)
                    throw new NotSupportedException("ConnectionInfo can only be used with IDbConnectionFactoryExtended");

                if (connInfo.ConnectionString != null && connInfo.ProviderName != null)
                    return dbFactoryExtended.OpenDbConnectionString(connInfo.ConnectionString, connInfo.ProviderName);

                if (connInfo.ConnectionString != null)
                    return dbFactoryExtended.OpenDbConnectionString(connInfo.ConnectionString);

                if (connInfo.NamedConnection != null)
                    return dbFactoryExtended.OpenDbConnection(connInfo.NamedConnection);
            }

            return dbFactory.OpenDbConnection();
        }

        public virtual IRedisClient GetRedisClient(IRequest req = null)
        {
            return Container.TryResolve<IRedisClientsManager>().GetClient();
        }

        public virtual ICacheClient GetCacheClient(IRequest req)
        {
            return this.GetCacheClient();
        }

        public virtual MemoryCacheClient GetMemoryCacheClient(IRequest req)
        {
            return Container.TryResolve<MemoryCacheClient>();
        }

        public virtual IMessageProducer GetMessageProducer(IRequest req = null)
        {
            return (Container.TryResolve<IMessageFactory>()
                ?? Container.TryResolve<IMessageService>().MessageFactory).CreateMessageProducer();
        }

        public virtual IServiceGateway GetServiceGateway(IRequest req)
        {
            var factory = Container.TryResolve<IServiceGatewayFactory>();
            return factory != null ? factory.GetServiceGateway(req) 
                : Container.TryResolve<IServiceGateway>()
                ?? new InProcessServiceGateway(req);
        }

        public virtual IAuthRepository GetAuthRepository(IRequest req = null)
        {
            return TryResolve<IAuthRepository>();
        }
    }

}