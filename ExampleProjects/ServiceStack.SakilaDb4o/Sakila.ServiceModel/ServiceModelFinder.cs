using ServiceStack.ServiceModel;

namespace Sakila.ServiceModel
{
	public class ServiceModelFinder : ServiceModelFinderBase
	{
		public static ServiceModelFinder Instance = new ServiceModelFinder();
	}

}