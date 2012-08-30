using System;
using NUnit.Framework;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Tests;

namespace ServiceStack.Auth.Tests
{
	[TestFixture, Explicit]
	public class SecureTests:TestBase
	{
		[Test]
		public void can_execute_secure_service ()
		{
			
			var secure= Client.Post<SecureResponse>("/secure",
			new Secure()
			{
				UserName="angel"
			});
			//fails against webhost-xsp2!,   runs fine against console host 
            //DB: Could be a result of certain webhosts hijacking particular HTTP Exceptions
			
			Console.WriteLine(secure.Dump());
						
		}
	}
}

// run ServiceStack.WebHostApp first with xsp2 
// http://127.0.0.1:8080/api/auth?UserName=test1&Password=test1&format=json  OK
// http://127.0.0.1:8080/api/secure?UserName=test1&format=json OK
// http://127.0.0.1:8080/api/auth/logout?format=json
