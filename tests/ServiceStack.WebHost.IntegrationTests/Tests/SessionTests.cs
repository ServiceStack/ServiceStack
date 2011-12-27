using System;
using NUnit.Framework;
using ServiceStack.Common;
using ServiceStack.Messaging;
using ServiceStack.ServiceInterface;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
	[TestFixture]
	public class SessionTests
	{
        [Test]		 
        public void Adhoc()
        {
        	var appliesTo = ApplyTo.Post | ApplyTo.Put;
			Console.WriteLine(appliesTo.ToString());
			Console.WriteLine(appliesTo.ToDescription());
			Console.WriteLine(string.Join(", ", appliesTo.ToList().ToArray()));
        }
	}
}