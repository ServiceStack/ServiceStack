namespace ServiceStack.Metadata
{
    public class Soap11WsdlTemplate : WsdlTemplateBase
    {
        public override string WsdlName => "Soap11";

        protected override string ReplyActionsTemplate =>
   @"<wsdl:operation name=""{1}"">
      <soap:operation soapAction=""{0}{1}"" style=""document"" />
      <wsdl:input>
        <soap:body use=""literal"" />
      </wsdl:input>
      <wsdl:output>
        <soap:body use=""literal"" />
      </wsdl:output>
    </wsdl:operation>";

        protected override string OneWayActionsTemplate =>
  @"<wsdl:operation name=""{1}"">
      <soap:operation soapAction=""{0}{1}"" style=""document"" />
      <wsdl:input>
        <soap:body use=""literal"" />
      </wsdl:input>
    </wsdl:operation>";

        protected override string ReplyBindingContainerTemplate =>
   @"<wsdl:binding name=""BasicHttpBinding_I{1}"" type=""svc:I{1}"">
        <soap:binding transport=""http://schemas.xmlsoap.org/soap/http"" />
        {0}
    </wsdl:binding>";

        protected override string OneWayBindingContainerTemplate =>
   @"<wsdl:binding name=""BasicHttpBinding_I{1}OneWay"" type=""svc:I{1}OneWay"">
        <soap:binding transport=""http://schemas.xmlsoap.org/soap/http"" />
        {0}
    </wsdl:binding>";

        protected override string ReplyEndpointUriTemplate =>
   @"<wsdl:service name=""{0}SyncReply"">
        <wsdl:port name=""BasicHttpBinding_I{2}"" binding=""svc:BasicHttpBinding_I{2}"">
            <soap:address location=""{1}"" />
        </wsdl:port>
    </wsdl:service>";

        protected override string OneWayEndpointUriTemplate =>
   @"<wsdl:service name=""{0}AsyncOneWay"">
        <wsdl:port name=""BasicHttpBinding_I{2}OneWay"" binding=""svc:BasicHttpBinding_I{2}OneWay"">
            <soap:address location=""{1}"" />
        </wsdl:port>
    </wsdl:service>";

    }
}