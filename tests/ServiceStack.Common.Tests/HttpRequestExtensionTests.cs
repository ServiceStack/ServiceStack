using System;
using System.Runtime.Serialization;
using NUnit.Framework;
using ServiceStack.ServiceHost;

namespace ServiceStack.Common.Tests
{
	[TestFixture]
	public class HttpRequestExtensionTests
	{
		[Test]
		public void ToStatusCode_returns_correct_http_status_codes()
		{
			Assert.That(new ArgumentException().ToStatusCode(), Is.EqualTo(400));
			Assert.That(new SerializationException().ToStatusCode(), Is.EqualTo(400));
			Assert.That(new UnauthorizedAccessException().ToStatusCode(), Is.EqualTo(403));
			Assert.That(new NotImplementedException().ToStatusCode(), Is.EqualTo(405));
			Assert.That(new NotSupportedException().ToStatusCode(), Is.EqualTo(405));
			Assert.That(new NotAcceptableException().ToStatusCode(), Is.EqualTo(406));
			Assert.That(new Exception().ToStatusCode(), Is.EqualTo(500));
		}
	}
}
