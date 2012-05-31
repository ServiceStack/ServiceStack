using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Plugins.ProtoBuf
{
	public class AppStart
	{
		public static void Start()
		{
			EndpointHost.AddPlugin(new ProtoBufFormat());
		}
	}
}