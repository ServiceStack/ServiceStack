using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Templates;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
    public class TemplateAutoQueryFilters : TemplateFilter, IAutoQueryDbFilters
    {
        private IHttpRequest req(TemplateScopeContext scope) => scope.GetValue("Request") as IHttpRequest;
        private ServiceStackHost appHost => HostContext.AppHost;

        public object sendToAutoQuery(TemplateScopeContext scope, string requestName) => 
            sendToAutoQuery(scope, TypeConstants.EmptyObjectDictionary, requestName, null);
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

                if (requestType.HasInterface(typeof(IQueryData)))
                {
                    var ssFillter = Context.TemplateFilters.FirstOrDefault(x => x is TemplateServiceStackFilters) as TemplateServiceStackFilters;
                    if (ssFillter == null)
                        throw new NotImplementedException("sendToAutoQuery Data requires TemplateServiceStackFilters");

                    return ssFillter.sendToAutoQuery(scope, dto, requestName, options);
                }
                
                var autoQuery = appHost.TryResolve<IAutoQueryDb>();
                if (autoQuery == null)
                    throw new NotSupportedException("The AutoQueryFeature plugin is not registered.");

                var objDictionary = dto is Dictionary<string, object> od ? od : null;
                
                var requestDto = objDictionary != null 
                    ? objDictionary.FromObjectDictionary(requestType)
                    : dto.GetType() == requestType
                        ? dto
                        : dto.ConvertTo(requestType);
                
                if (!(requestDto is IQueryDb aqDto))
                    throw new ArgumentException("Request DTO is not an AutoQuery DTO: " + requestName);

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
    }
}