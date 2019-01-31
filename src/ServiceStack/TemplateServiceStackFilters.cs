using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ServiceStack.Auth;
using ServiceStack.Host;
using ServiceStack.Templates;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
    public class TemplateServiceStackFilters : TemplateFilter
    {
        private ServiceStackHost appHost => HostContext.AppHost;

        public IHttpRequest getHttpRequest(TemplateScopeContext scope) => req(scope);
        private IHttpRequest req(TemplateScopeContext scope) => scope.GetValue("Request") as IHttpRequest;

        public object sendToGateway(TemplateScopeContext scope, object dto, string requestName) => sendToGateway(scope, dto, requestName, null);
        public object sendToGateway(TemplateScopeContext scope, object dto, string requestName, object options)
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

        public object publishToGateway(TemplateScopeContext scope, object dto, string requestName) => publishToGateway(scope, dto, requestName, null);
        public object publishToGateway(TemplateScopeContext scope, object dto, string requestName, object options)
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
        
        public object sendToAutoQuery(TemplateScopeContext scope, object dto, string requestName) => sendToAutoQuery(scope, dto, requestName, null);
        public object sendToAutoQuery(TemplateScopeContext scope, object dto, string requestName, object options)
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
                    var ssFilter = Context.TemplateFilters.FirstOrDefault(x => x is IAutoQueryDbFilters) as IAutoQueryDbFilters;
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
       
        public object getUserSession(TemplateScopeContext scope) => req(scope).GetSession();
        public IAuthSession userSession(TemplateScopeContext scope) => req(scope).GetSession();

        public bool isAuthenticated(TemplateScopeContext scope)
        {
            var authSession = userSession(scope);
            return authSession?.IsAuthenticated == true;
        }

        [HandleUnknownValue] public object ifAuthenticated(TemplateScopeContext scope) => isAuthenticated(scope) 
            ? (object)IgnoreResult.Value : StopExecution.Value;
       
        [HandleUnknownValue] public object ifNotAuthenticated(TemplateScopeContext scope) => !isAuthenticated(scope) 
            ? (object)IgnoreResult.Value : StopExecution.Value;
        
        [HandleUnknownValue] public object onlyIfAuthenticated(TemplateScopeContext scope, object value) => isAuthenticated(scope) 
            ? value : StopExecution.Value;

        [HandleUnknownValue] public object endIfAuthenticated(TemplateScopeContext scope, object value) => !isAuthenticated(scope) 
            ? value : StopExecution.Value;

        public IHttpResult getHttpResult(TemplateScopeContext scope, object options) => httpResult(scope, options);
        public HttpResult httpResult(TemplateScopeContext scope, object options)
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

        public ResponseStatus getErrorStatus(TemplateScopeContext scope) => 
            scope.GetValue("errorStatus") as ResponseStatus ??
            req(scope)?.GetItem(Keywords.ErrorStatus) as ResponseStatus;
        
        public string errorResponseSummary(TemplateScopeContext scope) => errorResponseSummary(scope, getErrorStatus(scope));
        public string errorResponseSummary(TemplateScopeContext scope, ResponseStatus errorStatus)
        {
            if (errorStatus == null)
                return null;

            return errorStatus.Errors.IsEmpty()
                ? errorStatus.Message ?? errorStatus.ErrorCode
                : null;
        }

        public string errorResponseExcept(TemplateScopeContext scope, IEnumerable<object> fields) =>
            errorResponseExcept(scope, getErrorStatus(scope), fields);
        public string errorResponseExcept(TemplateScopeContext scope, ResponseStatus errorStatus, IEnumerable<object> fields)
        {
            if (errorStatus == null)
                return null;
            
            var fieldNamesLookup = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var fieldNames = new List<string>();
            foreach (var field in fields)
            {
                var fieldName = field.AsString();
                fieldNamesLookup.Add(fieldName);
                fieldNames.Add(fieldName);
            }

            if (!fieldNames.IsEmpty() && !errorStatus.Errors.IsEmpty())
            {
                foreach (var fieldError in errorStatus.Errors)
                {
                    if (fieldNamesLookup.Contains(fieldError.FieldName))
                        return null;
                }

                var firstFieldError = errorStatus.Errors[0];
                return firstFieldError.Message ?? firstFieldError.ErrorCode;
            }

            return errorStatus.Message ?? errorStatus.ErrorCode;
        }

        public string errorResponse(TemplateScopeContext scope) => errorResponse(scope, getErrorStatus(scope), null);
        public string errorResponse(TemplateScopeContext scope, string fieldName) =>
            errorResponse(scope, getErrorStatus(scope), fieldName);
        public string errorResponse(TemplateScopeContext scope, ResponseStatus errorStatus, string fieldName)
        {
            if (fieldName == null)
                return errorResponseSummary(scope, errorStatus);
            if (errorStatus == null || errorStatus.Errors.IsEmpty())
                return null;

            foreach (var fieldError in errorStatus.Errors)
            {
                if (fieldName.EqualsIgnoreCase(fieldError.FieldName))
                    return fieldError.Message ?? fieldError.ErrorCode;
            }

            return null;
        }
    }
}