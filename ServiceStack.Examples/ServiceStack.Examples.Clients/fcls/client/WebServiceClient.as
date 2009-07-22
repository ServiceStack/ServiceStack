package com.ddn.potope.serviceModel.client
{
	import com.ddn.potope.util.LogUtils;
	
	import flash.events.ErrorEvent;
	
	import mx.logging.ILogger;
	import mx.rpc.AsyncToken;
	import mx.rpc.events.FaultEvent;
	import mx.rpc.http.HTTPService;
	import mx.rpc.xml.*;

	public class WebServiceClient 
	{
		private static var log:ILogger = LogUtils.getLogger("com.ddn.potope.serviceModel.client.WebServiceClient");
		
		public var baseUri:String;
		public var xmlSerializer:IXMLSerializer;
		public var service:HTTPService;
		private var lastOnError:Function; //TODO: we need to do this properly
		
		public function WebServiceClient(baseUri:String=null, xmlSerializer:IXMLSerializer=null) 
		{
			this.baseUri = baseUri;
			this.xmlSerializer = xmlSerializer;
			service = new HTTPService();
	        service.addEventListener(ErrorEvent.ERROR, onErrorHandler);
	        service.addEventListener(FaultEvent.FAULT, onErrorHandler); 
		}
		
		protected function onErrorHandler(e:*): void 
		{
			log.info("onErrorHandler, error is {0}" + e);
			if (lastOnError != null)
			{
				var faultEvent:FaultEvent = e as FaultEvent;
				if (faultEvent)
				{
					e = new ErrorEvent("FaultEvent", false, false, faultEvent.fault.message);					
				}
				lastOnError(e);				
			}
		}
				
		public function _post(methodName:String, postData:String, 
			onSuccess:Function=null, onError:Function=null) : AsyncToken
		{	
	        service.url = baseUri + methodName;
			service.method = "POST";
			service.resultFormat = "text";
			return _send(service, postData, onSuccess, onError);
		}
		
		protected function _get(methodName:String, urlParams:Object, 
			onSuccess:Function, onError:Function=null) : AsyncToken {
			var requestUrl:String = baseUri + methodName;
	        service.method = "GET";
	        service.url = requestUrl;
			service.resultFormat = "text";
	        return _send(service, urlParams, onSuccess, onError);
		}
		
		protected function _send(client:HTTPService, data:Object, 
			onSuccess:Function, onError:Function=null):AsyncToken
		{	
			log.info("_send(), request url: {0}" + service.url);
	        lastOnError = onError;
	        var token:AsyncToken = service.send(data);
	        token.onSuccess = onSuccess;
	        token.onError = onError;
	        return token;
		}        
				
	}                
}