using NUnit.Framework;
using ServiceStack.Examples.ServiceInterface;
using ServiceStack.Examples.ServiceModel.Operations;

namespace ServiceStack.Examples.Tests
{
	[TestFixture]
	public class GetFactorialTests : TestHostBase
	{
		[Test]
		public void GetFactorial_Test()
		{
			var request = new GetFactorial { ForNumber = 4 };

			var handler = new GetFactorialService();
			
			var response = (GetFactorialResponse)handler.Execute(request);

			Assert.That(response.Result, Is.EqualTo(4 * 3 * 2 * 1));
		}
	}
}