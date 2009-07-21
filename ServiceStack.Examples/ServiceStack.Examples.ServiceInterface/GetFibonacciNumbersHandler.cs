using System.Collections.Generic;
using ServiceStack.Examples.ServiceInterface.Types;
using ServiceStack.LogicFacade;
using ServiceStack.Service;
using ServiceStack.ServiceInterface;

namespace ServiceStack.Examples.ServiceInterface
{
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