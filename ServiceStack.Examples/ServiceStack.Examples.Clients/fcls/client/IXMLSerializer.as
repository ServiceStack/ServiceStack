package com.ddn.potope.serviceModel.client
{
	public interface IXMLSerializer
	{
	    function Parse(typeName:String, message:Object):XML;
	}
}