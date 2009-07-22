package com.ddn.potope.serviceModel.client
{
	import mx.rpc.AsyncToken;
	
	public interface IServiceClient
	{
		function getUrl(methodName:String, urlParams:Object, 
			onSuccess:Function, onError:Function=null) : AsyncToken;
				
		function send(methodName:String, message:Object, 
			onSuccess:Function=null, onError:Function=null) : AsyncToken;
			
	}
}