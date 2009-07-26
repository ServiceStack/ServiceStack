using System;
using ServiceStack.Examples.ServiceInterface.Types;

namespace ServiceStack.Examples.ServiceInterface.Tests.MonoTests
{
	public class GetUsersProgram : TestProgramBase
	{
		public static void Main()
		{
			InitApplicationContext();

			var handler = new GetUsersHandler();

			var response = (GetUsersResponse)handler.Execute(CreateOperationContext(
				new GetUsers {
					UserNames = new ArrayOfString("userName")
				}));

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