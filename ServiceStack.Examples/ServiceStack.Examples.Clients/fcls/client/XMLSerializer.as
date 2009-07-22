package com.ddn.potope.serviceModel.client
{
	import mx.rpc.xml.*;
	
	public class XMLSerializer extends XMLEncoder implements IXMLSerializer
	{
		private var _xmlEncoder:XMLEncoder;
		private var defaultNS:String;
		
		public function XMLSerializer(schemaManager:SchemaManager, defaultNS:String=null)
		{
			super.schemaManager = schemaManager;
			this.defaultNS = defaultNS;
		}

		public function Parse(typeName:String, message:Object):XML
		{
			var qName:QName = new QName(defaultNS, typeName);
			var qType:QName = new QName(defaultNS, typeName);
	        var result:XMLList = super.encode(message, qName, qType, null);
	        return result.length() > 0 ? result[0] : null;
		}
	}
}