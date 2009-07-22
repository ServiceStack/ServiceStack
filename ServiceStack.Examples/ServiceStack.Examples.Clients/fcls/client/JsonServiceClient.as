package com.ddn.potope.serviceModel.client
{
	import com.adobe.serialization.json.JSON;
	import com.ddn.potope.util.LogUtils;
	
	import mx.logging.ILogger;
	import mx.rpc.AsyncToken;
	import mx.rpc.events.ResultEvent;
	
	public class JsonServiceClient extends WebServiceClient implements IServiceClient
	{
		private static var log:ILogger = LogUtils.getLogger("com.ddn.potope.serviceModel.client.JsonServiceClient");

		public function JsonServiceClient(baseUri:String=null, xmlSerializer:IXMLSerializer=null)
		{
			super(baseUri, xmlSerializer);
	        service.addEventListener(ResultEvent.RESULT, onSuccessHandler);
		}

		public function getUrl(methodName:String, urlParams:Object, 
			onSuccess:Function, onError:Function=null) : AsyncToken
		{
			return _get(methodName, urlParams, onSuccess, onError);
		}

		public function send(methodName:String, message:Object, 
			onSuccess:Function=null, onError:Function=null) : AsyncToken
		{	
	        service.url = baseUri + methodName;
			service.method = "POST";
			service.contentType = "application/json";
			var json:String = JSON.encode(message);
			return _post(methodName, json, onSuccess, onError);
		}
				
		protected function onSuccessHandler(e:ResultEvent): void  
		{
			log.info("onSuccessHandler");
			var token:AsyncToken = e.token;
			var jsonObj:Object = JSON.decode(new String(e.result)); 
			if (token.onSuccess)
			{
				var jsonEvent:ResultEvent = ResultEvent.createEvent(jsonObj, e.token, e.message);	
				token.onSuccess(jsonEvent);
			}
		}
	}
}