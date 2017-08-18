using System;
using System.Collections.Generic;
using ServiceStack.Templates;
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
        
    }
}