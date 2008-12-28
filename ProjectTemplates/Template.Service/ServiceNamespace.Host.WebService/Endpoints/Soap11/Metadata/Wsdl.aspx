<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Wsdl.aspx.cs" ContentType="text/xml" Inherits="@ServiceNamespace@.Host.WebService.Endpoints.Soap11.Metadata.Wsdl" 
%><?xml version="1.0" encoding="utf-8"?>
<wsdl:definitions name="Soap11" 
    targetNamespace="http://services.ddnglobal.com/" 
    xmlns:svc="http://services.ddnglobal.com/" 
    xmlns:tns="http://schemas.ddnglobal.com/types/" 
    
    xmlns:wsdl="http://schemas.xmlsoap.org/wsdl/" 
    xmlns:soap="http://schemas.xmlsoap.org/wsdl/soap/" 
    xmlns:soap12="http://schemas.xmlsoap.org/wsdl/soap12/" 
    xmlns:wsu="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd" 
    xmlns:soapenc="http://schemas.xmlsoap.org/soap/encoding/" 
    xmlns:wsam="http://www.w3.org/2007/05/addressing/metadata" 
    xmlns:wsa="http://schemas.xmlsoap.org/ws/2004/08/addressing" 
    xmlns:wsp="http://schemas.xmlsoap.org/ws/2004/09/policy" 
    xmlns:wsap="http://schemas.xmlsoap.org/ws/2004/08/addressing/policy" 
    xmlns:xsd="http://www.w3.org/2001/XMLSchema" 
    xmlns:msc="http://schemas.microsoft.com/ws/2005/12/wsdl/contract" 
    xmlns:wsaw="http://www.w3.org/2006/05/addressing/wsdl" 
    xmlns:wsa10="http://www.w3.org/2005/08/addressing" 
    xmlns:wsx="http://schemas.xmlsoap.org/ws/2004/09/mex">

	<wsdl:types>
		<%= Xsd %>
	</wsdl:types>
	
<asp:Repeater id="repReplyMessages" runat="server">
    <ItemTemplate>
    <wsdl:message name="<%# Container.DataItem %>In">
        <wsdl:part name="parameters" element="tns:<%# Container.DataItem %>" />
    </wsdl:message>
    <wsdl:message name="<%# Container.DataItem %>Out">
        <wsdl:part name="parameters" element="tns:<%# Container.DataItem %>Response" />
    </wsdl:message>
    </ItemTemplate>
</asp:Repeater>
	
<asp:Repeater id="repOneWayMessages" runat="server">
    <ItemTemplate>
    <wsdl:message name="<%# Container.DataItem %>In">
        <wsdl:part name="parameters" element="tns:<%# Container.DataItem %>" />
    </wsdl:message>
    </ItemTemplate>
</asp:Repeater>

<wsdl:portType name="ISyncReply">
<asp:Repeater id="repReplyPortTypes" runat="server">
    <ItemTemplate>
        <wsdl:operation name="<%# Container.DataItem %>">
            <wsdl:input message="svc:<%# Container.DataItem %>In" />
            <wsdl:output message="svc:<%# Container.DataItem %>Out" />
        </wsdl:operation>
    </ItemTemplate>
</asp:Repeater>
</wsdl:portType>

<wsdl:portType name="IOneWay">
<asp:Repeater id="repOneWayPortTypes" runat="server">
    <ItemTemplate>
        <wsdl:operation name="<%# Container.DataItem %>">
            <wsdl:input message="svc:<%# Container.DataItem %>In" />
        </wsdl:operation>
    </ItemTemplate>
</asp:Repeater>
</wsdl:portType>

	<wsdl:binding name="BasicHttpBinding_ISyncReply" type="svc:ISyncReply">
        <soap:binding transport="http://schemas.xmlsoap.org/soap/http" />
        
        <asp:Repeater id="repReplyOperations" runat="server">
            <ItemTemplate>
            <wsdl:operation name="<%# Container.DataItem %>">
              <soap:operation soapAction="http://services.ddnglobal.com/<%# Container.DataItem %>" style="document" />
              <wsdl:input>
                <soap:body use="literal" />
              </wsdl:input>
              <wsdl:output>
                <soap:body use="literal" />
              </wsdl:output>
            </wsdl:operation>
            </ItemTemplate>
        </asp:Repeater>
	</wsdl:binding>
        
	<wsdl:binding name="BasicHttpBinding_IOneWay" type="svc:IOneWay">
        <soap:binding transport="http://schemas.xmlsoap.org/soap/http" />
        
        <asp:Repeater id="repOneWayOperations" runat="server">
            <ItemTemplate>
            <wsdl:operation name="<%# Container.DataItem %>">
              <soap:operation soapAction="http://services.ddnglobal.com/<%# Container.DataItem %>" style="document" />
              <wsdl:input>
                <soap:body use="literal" />
              </wsdl:input>
            </wsdl:operation>
            </ItemTemplate>
        </asp:Repeater>
	</wsdl:binding>

	<wsdl:service name="@ServiceName@SyncReply">
		<wsdl:port name="BasicHttpBinding_ISyncReply" binding="svc:BasicHttpBinding_ISyncReply">
			<soap:address location="<%= ReplyEndpointUri %>" />
		</wsdl:port>
	</wsdl:service>

	<wsdl:service name="@ServiceName@AsyncOneWay">
		<wsdl:port name="BasicHttpBinding_IOneWay" binding="svc:BasicHttpBinding_IOneWay">
			<soap:address location="<%= OneWayEndpointUri %>" />
		</wsdl:port>
	</wsdl:service>
	
</wsdl:definitions>