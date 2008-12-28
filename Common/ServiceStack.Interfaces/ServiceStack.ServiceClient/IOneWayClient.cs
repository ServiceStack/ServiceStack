namespace ServiceStack.ServiceClient
{
	public interface IOneWayClient
	{
		void SendOneWay(object request);
	}
}