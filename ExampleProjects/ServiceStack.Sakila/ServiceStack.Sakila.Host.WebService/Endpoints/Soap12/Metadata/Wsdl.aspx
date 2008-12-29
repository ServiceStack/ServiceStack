<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Wsdl.aspx.cs" ContentType="text/xml" Inherits="ServiceStack.Sakila.Host.WebService.Endpoints.Soap12.Metadata.Wsdl" 
%><?xml version="1.0" encoding="utf-8"?>
<wsdl:definitions name="Soap12" 
    targetNamespace="http://services.servicestack.net/" 
    xmlns:svc="http://services.servicestack.net/" 
    xmlns:tns="http://schemas.servicestack.net/types/" 
    
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

	<wsdl:binding name="WSHttpBinding_ISyncReply" type="svc:ISyncReply">
		<soap12:binding transport="http://schemas.xmlsoap.org/soap/http" />

        <asp:Repeater id="repReplyOperations" runat="server">
            <ItemTemplate>
            <wsdl:operation name="<%# Container.DataItem %>">
              <soap12:operation soapAction="http://services.servicestack.net/<%# Container.DataItem %>" style="document" />
              <wsdl:input>
                <soap12:body use="literal" />
              </wsdl:input>
              <wsdl:output>
                <soap12:body use="literal" />
              </wsdl:output>
            </wsdl:operation>
            </ItemTemplate>
        </asp:Repeater>

	</wsdl:binding>

	<wsdl:binding name="WSHttpBinding_IOneWay" type="svc:IOneWay">
        <soap:binding transport="http://schemas.xmlsoap.org/soap/http" />
        
        <asp:Repeater id="repOneWayOperations" runat="server">
            <ItemTemplate>
            <wsdl:operation name="<%# Container.DataItem %>">
              <soap12:operation soapAction="http://services.servicestack.net/<%# Container.DataItem %>" style="document" />
              <wsdl:input>
                <soap12:body use="literal" />
              </wsdl:input>
            </wsdl:operation>
            </ItemTemplate>
        </asp:Repeater>
	</wsdl:binding>

	<wsdl:service name="SyncReply">
		<wsdl:port name="WSHttpBinding_ISyncReply" binding="svc:WSHttpBinding_ISyncReply">
			<soap:address location="<%= ReplyEndpointUri %>" />
		</wsdl:port>
		<wsdl:port name="WSHttpBinding_IOneWay" binding="svc:WSHttpBinding_IOneWay">
			<soap:address location="<%= OneWayEndpointUri %>" />
		</wsdl:port>
	</wsdl:service>
	
</wsdl:definitions>
