using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;

namespace ServiceStack.Examples.ServiceInterface.Tests
{
	[TestFixture]
	public class GetFibonacciNumbersTests : TestBase
	{
		[Test]
		public void GetFibonacciNumbers_Test()
		{
			var request = new GetFibonacciNumbers { Take = 5 };

			var handler = new GetFibonacciNumbersHandler();

			var response = (GetFibonacciNumbersResponse)handler.Execute(CreateOperationContext(request));

			Assert.That(response.Results.Count, Is.EqualTo(request.Take));
			Assert.That(response.Results, Is.EqualTo(new[] { 1, 2, 3, 5, 8 }));
		}
	}

}
