using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Auth;
using ServiceStack.Templates;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
    // ReSharper disable InconsistentNaming
    
    public class TemplateServiceStackFilters : TemplateFilter
    {
        private IHttpRequest req(TemplateScopeContext scope) => scope.GetValue("Request") as IHttpRequest;
        private ServiceStackHost appHost => HostContext.AppHost;

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
                    var ssFillter = Context.TemplateFilters.FirstOrDefault(x => x is IAutoQueryDbFilters) as IAutoQueryDbFilters;
                    if (ssFillter == null)
                        throw new NotImplementedException("sendToAutoQuery RDBMS requires TemplateAutoQueryFilters");

                    return ssFillter.sendToAutoQuery(scope, dto, requestName, options);
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
       
        public IAuthSession userSession(TemplateScopeContext scope) => req(scope).GetSession();

        public bool isAuthenticated(TemplateScopeContext scope) => userSession(scope)?.IsAuthenticated == true;
        
        [HandleUnknownValue] public object ifAuthenticated(TemplateScopeContext scope) => isAuthenticated(scope) 
            ? (object)IgnoreResult.Value : StopExecution.Value;
       
        [HandleUnknownValue] public object ifNotAuthenticated(TemplateScopeContext scope) => !isAuthenticated(scope) 
            ? (object)IgnoreResult.Value : StopExecution.Value;
        
        [HandleUnknownValue] public object onlyIfAuthenticated(TemplateScopeContext scope, object value) => isAuthenticated(scope) 
            ? value : StopExecution.Value;

        [HandleUnknownValue] public object endIfAuthenticated(TemplateScopeContext scope, object value) => !isAuthenticated(scope) 
            ? value : StopExecution.Value;
    }
}