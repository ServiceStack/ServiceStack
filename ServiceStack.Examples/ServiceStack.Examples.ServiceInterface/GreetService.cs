using ServiceStack.Examples.ServiceModel.Operations;
using ServiceStack.ServiceHost;

namespace ServiceStack.Examples.ServiceInterface
{
	/// <summary>
	/// An example of a very basic web service
	/// </summary>
	public class GreetService : IService<Greet>
	{
		public object Execute(Greet request)
		{
			return new GreetResponse { Result = "Hello " + request.Name };
		}
	}
}