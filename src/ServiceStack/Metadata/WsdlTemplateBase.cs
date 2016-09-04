using System.Collections.Generic;
using ServiceStack.Text;

namespace ServiceStack.Metadata
{
    public abstract class WsdlTemplateBase
    {
        public string Xsd { get; set; }
        public string ServiceName { get; set; }
        public IList<string> ReplyOperationNames { get; set; }
        public IList<string> OneWayOperationNames { get; set; }
        public string ReplyEndpointUri { get; set; }
        public string OneWayEndpointUri { get; set; }

        public abstract string WsdlName { get; }

        protected virtual string ReplyMessagesTemplate => 
  @"<wsdl:message name=""{0}In"">
        <wsdl:part name=""par"" element=""tns:{0}"" />
    </wsdl:message>
    <wsdl:message name=""{0}Out"">
        <wsdl:part name=""par"" element=""tns:{0}Response"" />
    </wsdl:message>";

        protected virtual string OneWayMessagesTemplate => 
  @"<wsdl:message name=""{0}In"">
        <wsdl:part name=""par"" element=""tns:{0}"" />
    </wsdl:message>";

        protected virtual string ReplyOperationsTemplate => 
  @"<wsdl:operation name=""{0}"">
        <wsdl:input message=""svc:{0}In"" />
        <wsdl:output message=""svc:{0}Out"" />
    </wsdl:operation>";

        protected virtual string OneWayOperationsTemplate => 
   @"<wsdl:operation name=""{0}"">
        <wsdl:input message=""svc:{0}In"" />
    </wsdl:operation>";

        protected virtual string ReplyActionsTemplate => 
  @"<wsdl:operation name=""{1}"">
      <soap:operation soapAction=""{0}{1}"" style=""document"" />
      <wsdl:input>
        <soap:body use=""literal"" />
      </wsdl:input>
      <wsdl:output>
        <soap:body use=""literal"" />
      </wsdl:output>
    </wsdl:operation>";

        protected virtual string OneWayActionsTemplate => 
  @"<wsdl:operation name=""{1}"">
      <soap:operation soapAction=""{0}{1}"" style=""document"" />
      <wsdl:input>
        <soap:body use=""literal"" />
      </wsdl:input>
    </wsdl:operation>";

        protected abstract string ReplyBindingContainerTemplate { get; }
        protected abstract string OneWayBindingContainerTemplate { get; }
        protected abstract string ReplyEndpointUriTemplate { get; }
        protected abstract string OneWayEndpointUriTemplate { get; }

        private const string Template =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<wsdl:definitions name=""{0}"" 
    targetNamespace=""{10}"" 
    xmlns:svc=""{10}"" 
    xmlns:tns=""{10}"" 
    
    xmlns:wsdl=""http://schemas.xmlsoap.org/wsdl/"" 
    xmlns:soap=""http://schemas.xmlsoap.org/wsdl/soap/"" 
    xmlns:soap12=""http://schemas.xmlsoap.org/wsdl/soap12/"" 
    xmlns:wsu=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd"" 
    xmlns:soapenc=""http://schemas.xmlsoap.org/soap/encoding/"" 
    xmlns:wsam=""http://www.w3.org/2007/05/addressing/metadata"" 
    xmlns:wsa=""http://schemas.xmlsoap.org/ws/2004/08/addressing"" 
    xmlns:wsp=""http://schemas.xmlsoap.org/ws/2004/09/policy"" 
    xmlns:wsap=""http://schemas.xmlsoap.org/ws/2004/08/addressing/policy"" 
    xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" 
    xmlns:msc=""http://schemas.microsoft.com/ws/2005/12/wsdl/contract"" 
    xmlns:wsaw=""http://www.w3.org/2006/05/addressing/wsdl"" 
    xmlns:wsa10=""http://www.w3.org/2005/08/addressing"" 
    xmlns:wsx=""http://schemas.xmlsoap.org/ws/2004/09/mex"">

	<wsdl:types>
		{1}
	</wsdl:types>

	{2}

	{3}

	{4}

	{5}

	{6}
        
	{7}

	{8}

	{9}
	
</wsdl:definitions>";

        public string RepeaterTemplate(string template, IEnumerable<string> dataSource)
        {
            var sb = StringBuilderCache.Allocate();
            foreach (var item in dataSource)
            {
                sb.AppendFormat(template, item);
            }
            return StringBuilderCache.ReturnAndFree(sb);
        }

        public string RepeaterTemplate(string template, object arg0, IEnumerable<string> dataSource)
        {
            var sb = StringBuilderCache.Allocate();
            foreach (var item in dataSource)
            {
                sb.AppendFormat(template, arg0, item);
            }
            return StringBuilderCache.ReturnAndFree(sb);
        }

        public override string ToString()
        {
            var wsdlSoapActionNamespace = HostContext.Config.WsdlServiceNamespace;
            if (!wsdlSoapActionNamespace.EndsWith("/"))
                wsdlSoapActionNamespace += '/';

            var replyMessages = RepeaterTemplate(this.ReplyMessagesTemplate, this.ReplyOperationNames);
            var replyOperations = RepeaterTemplate(this.ReplyOperationsTemplate, this.ReplyOperationNames);
            var replyServiceName = (ServiceName ?? "SyncReply");
            replyOperations = "<wsdl:portType name=\"I" + replyServiceName + "\">" + replyOperations + "</wsdl:portType>";
            var replyActions = RepeaterTemplate(this.ReplyActionsTemplate, wsdlSoapActionNamespace, this.ReplyOperationNames);
            var replyBindings = string.Format(this.ReplyBindingContainerTemplate, replyActions, replyServiceName);
            var replyEndpointUri = string.Format(this.ReplyEndpointUriTemplate, ServiceName, this.ReplyEndpointUri, replyServiceName);

            var oneWayMessages = "";
            var oneWayOperations = "";
            var oneWayBindings = "";
            var oneWayEndpointUri = "";
            if (OneWayOperationNames.Count > 0)
            {
                oneWayMessages = RepeaterTemplate(this.OneWayMessagesTemplate, this.OneWayOperationNames);
                oneWayOperations = RepeaterTemplate(this.OneWayOperationsTemplate, this.OneWayOperationNames);
                var oneWayServiceName = (ServiceName ?? "");
                oneWayOperations = "<wsdl:portType name=\"I" + oneWayServiceName + "OneWay\">" + oneWayOperations + "</wsdl:portType>";
                var oneWayActions = RepeaterTemplate(this.OneWayActionsTemplate, wsdlSoapActionNamespace, this.OneWayOperationNames);
                oneWayBindings = string.Format(this.OneWayBindingContainerTemplate, oneWayActions, oneWayServiceName);
                oneWayEndpointUri = string.Format(this.OneWayEndpointUriTemplate, ServiceName, this.OneWayEndpointUri, oneWayServiceName);
            }

            var wsdl = string.Format(Template,
                WsdlName,
                Xsd,
                replyMessages,
                oneWayMessages,
                replyOperations,
                oneWayOperations,
                replyBindings,
                oneWayBindings,
                replyEndpointUri,
                oneWayEndpointUri,
                HostContext.Config.WsdlServiceNamespace);

            return wsdl;
        }
    }
}