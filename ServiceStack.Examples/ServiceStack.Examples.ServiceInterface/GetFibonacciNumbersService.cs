using System.Collections.Generic;
using ServiceStack.Configuration;
using ServiceStack.Examples.ServiceInterface.Types;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;

namespace ServiceStack.Examples.ServiceInterface
{
	/// <summary>
	/// The service handler that will be used to execute the request.
	/// 
	/// This purpose of this example is how you would implement a slightly more advanced
	/// web service returning a slightly more 'complex object'.
	/// </summary>
	public class GetFibonacciNumbersService 
		: IService<GetFibonacciNumbers>
	{
		private readonly ExampleConfig config;

		//Example of ServiceStack's built-in Funq IOC constructor injection
		public GetFibonacciNumbersService(ExampleConfig config)
		{
			this.config = config;
		}

		public object Execute(GetFibonacciNumbers request)
		{
			var skip = request.Skip.GetValueOrDefault(0);
			var take = request.Take.GetValueOrDefault(config.DefaultFibonacciLimit);

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