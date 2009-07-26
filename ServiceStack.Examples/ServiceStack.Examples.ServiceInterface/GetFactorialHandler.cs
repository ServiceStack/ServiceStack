using ServiceStack.Examples.ServiceInterface.Types;
using ServiceStack.LogicFacade;
using ServiceStack.Service;
using ServiceStack.ServiceInterface;

namespace ServiceStack.Examples.ServiceInterface
{
	/// <summary>
	/// The service or 'Port' handler that will be used to execute the request.
	/// The 'Port' attribute is used to link the 'service request' to the 'service implementation'
	/// 
	/// The purpose of this example is to show the minimum number and detail of classes 
	/// required in order to implement a simple service.
	/// </summary>
[Port(typeof(GetFactorial))]
public class GetFactorialHandler : IService
{
	public object Execute(IOperationContext context)
	{
		var request = context.Request.Get<GetFactorial>();

		return new GetFactorialResponse { Result = GetFactorial(request.ForNumber) };
	}

	static long GetFactorial(long n)
	{
		return n > 1 ? n * GetFactorial(n - 1) : 1;
	}
}
}