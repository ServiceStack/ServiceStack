namespace ServiceStack.WebHost.Endpoints.Support.Templates
{
	public class Soap12WsdlTemplate : WsdlTemplateBase
	{
		public override string WsdlName
		{
			get { return "Soap12"; }
		}

		protected override string ReplyActionsTemplate
		{
			get
			{
				return
	@"<wsdl:operation name=""{0}"">
      <soap:operation soapAction=""http://schemas.servicestack.net/types/{0}"" style=""document"" />
      <wsdl:input>
        <soap:body use=""literal"" />
      </wsdl:input>
      <wsdl:output>
        <soap:body use=""literal"" />
      </wsdl:output>
    </wsdl:operation>";
			}
		}

		protected override string OneWayActionsTemplate
		{
			get
			{
				return
	@"<wsdl:operation name=""{0}"">
      <soap:operation soapAction=""http://schemas.servicestack.net/types/{0}"" style=""document"" />
      <wsdl:input>
        <soap:body use=""literal"" />
      </wsdl:input>
    </wsdl:operation>";
			}
		}

		protected override string ReplyBindingContainerTemplate
		{
			get
			{
				return
	@"<wsdl:binding name=""WSHttpBinding_ISyncReply"" type=""svc:ISyncReply"">
		<soap12:binding transport=""http://schemas.xmlsoap.org/soap/http"" />
		{0}
	</wsdl:binding>";
			}
		}

		protected override string OneWayBindingContainerTemplate
		{
			get
			{
				return
	@"<wsdl:binding name=""WSHttpBinding_IOneWay"" type=""svc:IOneWay"">
		<soap12:binding transport=""http://schemas.xmlsoap.org/soap/http"" />
		{0}
	</wsdl:binding>";
			}
		}

		protected override string ReplyEndpointUriTemplate
		{
			get
			{
				return
	@"<wsdl:service name=""{0}SyncReply"">
		<wsdl:port name=""WSHttpBinding_ISyncReply"" binding=""svc:WSHttpBinding_ISyncReply"">
			<soap:address location=""{1}"" />
		</wsdl:port>
	</wsdl:service>";
			}
		}

		protected override string OneWayEndpointUriTemplate
		{
			get
			{
				return
	@"<wsdl:service name=""{0}AsyncOneWay"">
		<wsdl:port name=""WSHttpBinding_IOneWay"" binding=""svc:WSHttpBinding_IOneWay"">
			<soap:address location=""{1}"" />
		</wsdl:port>
	</wsdl:service>";
			}
		}

	}
}