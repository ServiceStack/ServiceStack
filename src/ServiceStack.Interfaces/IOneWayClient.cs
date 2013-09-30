namespace ServiceStack
{
	public interface IOneWayClient
	{
        void SendOneWay(object request);
        
        void SendOneWay(string relativeOrAbsoluteUrl, object request);
    }
}