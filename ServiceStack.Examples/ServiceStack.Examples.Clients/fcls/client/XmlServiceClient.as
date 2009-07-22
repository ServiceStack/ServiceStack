package com.ddn.potope.serviceModel.client
{
	import com.ddn.potope.util.LogUtils;
	
	import flash.events.ErrorEvent;
	
	import mx.logging.ILogger;
	import mx.rpc.AsyncToken;
	import mx.rpc.events.ResultEvent;
	
	public class XmlServiceClient extends WebServiceClient implements IServiceClient
	{
		private static var log:ILogger = LogUtils.getLogger("com.ddn.potope.serviceModel.client.XmlServiceClient");

		public function XmlServiceClient(baseUri:String=null, xmlSerializer:IXMLSerializer=null)
		{
			super(baseUri, xmlSerializer);
	        super.service.addEventListener(ResultEvent.RESULT, onSuccessHandler);
		}
		
		public function getUrl(methodName:String, urlParams:Object, 
			onSuccess:Function, onError:Function=null) : AsyncToken
		{
			return _get(methodName, urlParams, onSuccess, onError);
		}
				
		public function send(methodName:String, message:Object, 
			onSuccess:Function=null, onError:Function=null) : AsyncToken
		{	
			service.contentType = "text/xml";
			var xml:XML = xmlSerializer.Parse(methodName, message);
			return _post(methodName, xml.toString(), onSuccess, onError);
		}
		
		protected function onSuccessHandler(e:ResultEvent): void 
		{
			log.info(new Date() + ": received success response: ");
			var token:AsyncToken = e.token;
			
			var xmlText:String = e.result as String;
			var firstChar:String = xmlText.substr(0,1); 
        	if (firstChar != "<")
        	{
        		var error:ErrorEvent = new ErrorEvent("XmlServiceClient", false, false, 
        			"Response is not valid XML, firstChar is: '" + firstChar + "'");
        		log.error(error.text);
        		if (token.onError)
        		{
        			token.onError(error);
        		}
        		return;
        	}

			var xml:XML = createXml(xmlText);	
			if (token.onSuccess)
			{
				var xmlEvent:ResultEvent = ResultEvent.createEvent(xml, e.token, e.message);	
				token.onSuccess(xmlEvent);
			}
		}

        public static function createXml(xmlText:String): XML
        {
        	//strip out our default namespace to make it easier to navigate.
        	xmlText = xmlText.replace(" xmlns=\"http://schemas.ddnglobal.com/types/\"","");
        	return new XML(xmlText);
      	}
      	
	}

}