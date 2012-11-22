namespace ServiceStack.Service
{
	public interface IOneWayClient : IMayRequireCredentials
	{
        void SendOneWay(object request);
        
        void SendOneWay(string relativeOrAbsoluteUrl, object request);
    }
}