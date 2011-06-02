using System;
using System.Linq.Expressions;
using NUnit.Framework;
using ServiceStack.WebHost.EndPoints.Support.Markdown;

namespace ServiceStack.ServiceHost.Tests.Formats
{
	[TestFixture]
	public class ExpressionTests
	{
		[Test]
		public void Can_accesss_static_property()
		{
			var expr = "DateTime.Now.Year";
			var fn = DataBinder.CompileStaticAccessToString(expr);
			var result = fn();

			Console.WriteLine(result);
		}

	}
}