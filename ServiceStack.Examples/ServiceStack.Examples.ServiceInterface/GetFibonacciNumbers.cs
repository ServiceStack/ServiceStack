using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack.Examples.ServiceInterface.Types;
using ServiceStack.LogicFacade;
using ServiceStack.Service;
using ServiceStack.ServiceInterface;

namespace ServiceStack.Examples.ServiceInterface
{
	/* Below is a simple example on how to create a simple Web Service.
	 * It lists all the classes required to implement the 'GetFibonacciNumbers' Service. 	
	 */

	/// <summary>
	/// Use Plain old DataContract's Define your 'Service Interface'
	/// </summary>
	[DataContract(Namespace = "http://schemas.sericestack.net/examples/types")]
	public class GetFibonacciNumbers
	{
		[DataMember]
		public long? Skip { get; set; }

		[DataMember]
		public long? Take { get; set; }
	}

	[DataContract(Namespace = "http://schemas.sericestack.net/examples/types")]
	public class GetFibonacciNumbersResponse
	{
		[DataMember]
		public ArrayOfLong Results { get; set; }
	}


	/// <summary>
	/// The service or 'Port' handler that will be used to execute the request.
	/// 
	/// The 'Port' attribute is used to link the 'service request' to the 'service implementation'
	/// </summary>
	[Port(typeof(GetFibonacciNumbers))]
	public class GetFibonacciNumbersHandler : IService
	{
		public object Execute(IOperationContext context)
		{
			var request = context.Request.Get<GetFibonacciNumbers>();

			//An example of a service utilizing a 'provider' from the ApplicationContext.
			var defaultLimit = context.Application.Resources.Get<long>("DefaultFibonacciLimit", 10);

			var skip = request.Skip.GetValueOrDefault(0);
			var take = request.Take.GetValueOrDefault(defaultLimit);

			var results = new List<long>();

			var i = 0;
			foreach (var fibonacciNumber in GetFibonacciNumbersNumbers())
			{
				if (i++ < skip) continue;

				results.Add(fibonacciNumber);

				if (results.Count == take) break;
			}

			return new GetFibonacciNumbersResponse { Results = new ArrayOfLong(results) };
		}

		static IEnumerable<long> GetFibonacciNumbersNumbers()
		{
			long n1 = 0;
			long n2 = 1;

			while (true)
			{
				var n3 = n1 + n2;
				yield return n3;
				n1 = n2;
				n2 = n3;
			}
		}

	}

}