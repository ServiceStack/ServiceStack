using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Configuration;
using ServiceStack.Examples.ServiceInterface.Types;

namespace ServiceStack.Examples.ServiceInterface.Tests
{
	[TestFixture]
	public class GetFibonacciNumbersTests 
		: TestHostBase
	{
		[Test]
		public void GetFibonacciNumbers_Test()
		{
			var request = new GetFibonacciNumbers { Take = 5 };

			var handler = new GetFibonacciNumbersService(Container.Resolve<IResourceManager>());

			var response = (GetFibonacciNumbersResponse)
				handler.Execute(request);

			Assert.That(response.Results.Count, Is.EqualTo(request.Take));
			Assert.That(response.Results, Is.EqualTo(new[] { 1, 2, 3, 5, 8 }));
		}
	}

}
