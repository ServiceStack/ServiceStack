namespace ServiceStack.Service
{
	public interface IOneWayClient
	{
		void SendOneWay(object request);
	}
}