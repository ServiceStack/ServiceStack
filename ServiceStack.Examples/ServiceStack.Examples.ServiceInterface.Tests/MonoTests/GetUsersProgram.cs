using System;
using ServiceStack.DataAccess.Db4oProvider;
using ServiceStack.Examples.ServiceInterface.Types;

namespace ServiceStack.Examples.ServiceInterface.Tests.MonoTests
{
	public class GetUsersProgram : TestProgramBase
	{
		public static void Main()
		{
			var testConfig = new ExampleConfig { ConnectionString = ":memory:" };

			var service = new GetUsersService { Config = testConfig };

			var response = (GetUsersResponse)service.Execute(
				new GetUsers {
					UserNames = new ArrayOfString("userName")
				});

			if (response.Users.Count > 0)
			{
				var user = response.Users[0];
				Console.WriteLine("User: {0}, {1}, {2}", user.UserName, user.Password, user.Email);
			}
			else
			{
				Console.WriteLine("User does not exist");
			}
		}
	}
}