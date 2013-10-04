namespace ServiceStack
{
	public interface IOneWayClient
	{
        void SendOneWay(object requestDto);
        
        void SendOneWay(string relativeOrAbsoluteUrl, object request);
    }
}