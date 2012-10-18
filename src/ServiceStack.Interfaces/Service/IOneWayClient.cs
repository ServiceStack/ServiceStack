namespace ServiceStack.Service
{
	public interface IOneWayClient
	{
        void SendOneWay(object request);
        
        void SendOneWay(string relativeOrAbsoluteUrl, object request);
    }
}