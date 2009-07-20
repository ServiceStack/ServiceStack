using System.Runtime.Serialization;
using ServiceStack.LogicFacade;
using ServiceStack.Service;
using ServiceStack.ServiceInterface;

namespace ServiceStack.Examples.ServiceInterface
{
	/* Below is a simple example on how to create a simple Web Service.
	 * It lists all the classes required to implement the 'GetFactorial' Service. 	
	 */

	/// <summary>
	/// Use Plain old DataContract's Define your 'Service Interface'
	/// </summary>
	[DataContract(Namespace = "http://schemas.sericestack.net/examples/types")]
	public class GetFactorial
	{
		[DataMember]
		public long ForNumber { get; set; }
	}

	[DataContract(Namespace = "http://schemas.sericestack.net/examples/types")]
	public class GetFactorialResponse
	{
		[DataMember]
		public long Result { get; set; }
	}


	/// <summary>
	/// The service or 'Port' handler that will be used to execute the request.
	/// 
	/// The 'Port' attribute is used to link the 'service request' to the 'service implementation'
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